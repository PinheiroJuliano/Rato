using System;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace RatinhoDesktop.Services;

/// <summary>
/// Captura o áudio que está tocando no computador (loopback do dispositivo de saída
/// padrão, via WASAPI) e faz uma detecção simples de batida baseada em energia do sinal:
/// sempre que a energia de um pequeno bloco de áudio ultrapassa consideravelmente a média
/// recente, consideramos que houve uma "batida".
///
/// Isso não é uma análise musical sofisticada (não identifica BPM exato nem tipo de
/// instrumento), mas funciona bem o suficiente para dar a impressão de que o bichinho está
/// "sentindo" o ritmo de qualquer áudio que esteja tocando no Windows.
/// </summary>
public sealed class AudioReactiveService : IDisposable
{
    public event Action? BeatDetected;

    /// <summary>Intensidade (0.0 a 1.0+) do pico que gerou a última batida.</summary>
    public double LastBeatIntensity { get; private set; } = 1.0;

    private WasapiLoopbackCapture? _capture;
    private double _runningAverageEnergy;
    private DateTime _lastBeatTime = DateTime.MinValue;
    private bool _isRunning;

    // Intervalo mínimo entre batidas para não disparar múltiplas vezes na mesma nota
    // (equivale a no máximo ~5 batidas por segundo, o que cobre até músicas bem rápidas).
    private static readonly TimeSpan MinBeatInterval = TimeSpan.FromMilliseconds(180);

    public bool IsRunning => _isRunning;

    /// <summary>
    /// Inicia a captura do áudio do sistema. Retorna false (sem lançar exceção) se não
    /// houver dispositivo de saída disponível ou se o Windows negar o acesso ao loopback.
    /// </summary>
    public bool Start()
    {
        if (_isRunning) return true;

        try
        {
            var enumerator = new MMDeviceEnumerator();
            MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            _capture = new WasapiLoopbackCapture(device);
            _capture.DataAvailable += OnDataAvailable;
            _capture.RecordingStopped += (s, e) => _isRunning = false;

            _runningAverageEnergy = 0;
            _capture.StartRecording();
            _isRunning = true;
            return true;
        }
        catch
        {
            // Sem dispositivo de áudio, sem permissão, ou driver incompatível.
            Stop();
            return false;
        }
    }

    public void Stop()
    {
        _isRunning = false;

        if (_capture != null)
        {
            try
            {
                _capture.DataAvailable -= OnDataAvailable;
                _capture.StopRecording();
            }
            catch
            {
                // Ignora erros ao parar
            }
            finally
            {
                _capture.Dispose();
                _capture = null;
            }
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_capture == null) return;

        // O loopback do WASAPI normalmente entrega float 32 bits.
        int bytesPerSample = _capture.WaveFormat.BitsPerSample / 8;
        int channels = Math.Max(1, _capture.WaveFormat.Channels);
        if (bytesPerSample <= 0) return;

        int sampleCount = e.BytesRecorded / bytesPerSample;
        if (sampleCount <= 0) return;

        double sumSquares = 0;
        int consideredSamples = 0;

        if (_capture.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat && bytesPerSample == 4)
        {
            for (int i = 0; i + 4 <= e.BytesRecorded; i += 4)
            {
                float sample = BitConverter.ToSingle(e.Buffer, i);
                sumSquares += sample * sample;
                consideredSamples++;
            }
        }
        else if (bytesPerSample == 2)
        {
            for (int i = 0; i + 2 <= e.BytesRecorded; i += 2)
            {
                short sample = BitConverter.ToInt16(e.Buffer, i);
                double normalized = sample / 32768.0;
                sumSquares += normalized * normalized;
                consideredSamples++;
            }
        }
        else
        {
            return; // formato não suportado, ignora este bloco
        }

        if (consideredSamples == 0) return;

        double rms = Math.Sqrt(sumSquares / consideredSamples);

        // Média móvel lenta que representa o "volume médio recente" do áudio.
        // Isso permite que a detecção se adapte tanto a músicas altas quanto baixas.
        const double smoothing = 0.05;
        if (_runningAverageEnergy <= 0)
        {
            _runningAverageEnergy = rms;
        }
        else
        {
            _runningAverageEnergy = (smoothing * rms) + ((1 - smoothing) * _runningAverageEnergy);
        }

        // Só considera "batida" se houver som audível (evita disparos com silêncio/ruído de fundo)
        // e se o pico atual for consideravelmente maior que a média recente.
        const double minAudibleRms = 0.02;
        const double beatThresholdRatio = 1.35;

        bool loudEnough = rms > minAudibleRms;
        bool isPeak = rms > _runningAverageEnergy * beatThresholdRatio;
        bool intervalOk = (DateTime.UtcNow - _lastBeatTime) >= MinBeatInterval;

        if (loudEnough && isPeak && intervalOk)
        {
            _lastBeatTime = DateTime.UtcNow;
            LastBeatIntensity = Math.Min(2.0, rms / Math.Max(0.0001, _runningAverageEnergy));
            BeatDetected?.Invoke();
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
