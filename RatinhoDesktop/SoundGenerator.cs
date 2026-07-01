using System;
using System.IO;
using System.Text;
using RatinhoDesktop.Models;

namespace RatinhoDesktop;

public static class SoundGenerator
{
    /// <summary>
    /// Gera (uma única vez, com cache em disco) o som de clique correspondente a cada
    /// personagem, para que cada bichinho selecionado tenha um efeito sonoro próprio
    /// sem precisar de nenhum arquivo de áudio externo.
    /// </summary>
    public static string GenerateCharacterSoundFile(string targetDir, SoundCharacter character)
    {
        string fileName = character switch
        {
            SoundCharacter.Squeak => "squeak.wav",
            SoundCharacter.Moo => "moo.wav",
            SoundCharacter.Meow => "meow.wav",
            SoundCharacter.Pop => "pop.wav",
            SoundCharacter.Chime => "chime.wav",
            _ => "squeak.wav"
        };

        string filePath = Path.Combine(targetDir, fileName);
        if (File.Exists(filePath))
            return filePath;

        try
        {
            Directory.CreateDirectory(targetDir);

            short[] pcm = character switch
            {
                SoundCharacter.Squeak => BuildSqueakPcm(),
                SoundCharacter.Moo => BuildMooPcm(),
                SoundCharacter.Meow => BuildMeowPcm(),
                SoundCharacter.Pop => BuildPopPcm(),
                SoundCharacter.Chime => BuildChimePcm(),
                _ => BuildSqueakPcm()
            };

            byte[] wavBytes = CreateWav(pcm, 44100);
            File.WriteAllBytes(filePath, wavBytes);
        }
        catch
        {
            // Fallback: ignore file writing errors
        }

        return filePath;
    }

    private static short[] BuildSqueakPcm()
    {
        int sampleRate = 44100;
        double duration = 0.15;
        int numSamples = (int)(sampleRate * duration);
        short[] pcm = new short[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / sampleRate;
            double frequency = 1200 + (2000 * (t / duration));
            double volume = Math.Sin(Math.PI * t / duration);
            double angle = 2 * Math.PI * frequency * t;
            pcm[i] = (short)(25000 * volume * Math.Sin(angle));
        }

        return pcm;
    }

    private static short[] BuildMooPcm()
    {
        // Som grave e prolongado, com vibrato, lembrando um "muu".
        int sampleRate = 44100;
        double duration = 0.65;
        int numSamples = (int)(sampleRate * duration);
        short[] pcm = new short[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / sampleRate;
            double progress = t / duration;

            // Frequência base cai suavemente (110Hz -> 85Hz) com um leve vibrato.
            double vibrato = Math.Sin(2 * Math.PI * 6.0 * t) * 4.0;
            double frequency = (110 - (25 * progress)) + vibrato;

            // Envelope: ataque rápido, sustentação, e decaimento no final.
            double volume = Math.Min(1.0, t / 0.05) * Math.Min(1.0, (duration - t) / 0.2);

            // Onda dente-de-serra simplificada (harmônicos extras) para timbre mais "grave/rouco".
            double angle = 2 * Math.PI * frequency * t;
            double fundamental = Math.Sin(angle);
            double secondHarmonic = 0.4 * Math.Sin(2 * angle);
            double thirdHarmonic = 0.2 * Math.Sin(3 * angle);

            pcm[i] = (short)(18000 * volume * (fundamental + secondHarmonic + thirdHarmonic));
        }

        return pcm;
    }

    private static short[] BuildMeowPcm()
    {
        // Duas sílabas curtas com curva de pitch subindo e depois descendo, lembrando um miado.
        int sampleRate = 44100;
        double duration = 0.4;
        int numSamples = (int)(sampleRate * duration);
        short[] pcm = new short[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / sampleRate;
            double progress = t / duration;

            // Curva de pitch em "sino": sobe até a metade e desce (formato de miado).
            double frequency = 500 + (500 * Math.Sin(Math.PI * progress));

            double volume = Math.Sin(Math.PI * progress);
            double angle = 2 * Math.PI * frequency * t;

            pcm[i] = (short)(20000 * volume * Math.Sin(angle));
        }

        return pcm;
    }

    private static short[] BuildPopPcm()
    {
        // Blip curto e alegre, usado para bichinhos "genéricos" (dança/limpeza/etc).
        int sampleRate = 44100;
        double duration = 0.12;
        int numSamples = (int)(sampleRate * duration);
        short[] pcm = new short[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / sampleRate;
            double frequency = 900 - (400 * (t / duration));
            double volume = Math.Sin(Math.PI * t / duration);
            double angle = 2 * Math.PI * frequency * t;
            pcm[i] = (short)(22000 * volume * Math.Sin(angle));
        }

        return pcm;
    }

    private static short[] BuildChimePcm()
    {
        // Som tipo "sininho", com fundamental + oitava, e decaimento exponencial.
        int sampleRate = 44100;
        double duration = 0.45;
        int numSamples = (int)(sampleRate * duration);
        short[] pcm = new short[numSamples];

        double baseFreq = 1046.5; // C6

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / sampleRate;
            double volume = Math.Exp(-6.0 * t / duration);

            double fundamental = Math.Sin(2 * Math.PI * baseFreq * t);
            double octave = 0.5 * Math.Sin(2 * Math.PI * baseFreq * 2 * t);

            pcm[i] = (short)(20000 * volume * (fundamental + octave));
        }

        return pcm;
    }

    public static string GenerateMelodyFile(string targetDir)
    {
        string filePath = Path.Combine(targetDir, "melody.wav");
        if (File.Exists(filePath))
            return filePath;

        try
        {
            Directory.CreateDirectory(targetDir);
            int sampleRate = 22050; // Lower sample rate for retro feel
            double noteDuration = 0.22; // Seconds per beat
            
            // Frequencies for a cute retro melody loop
            // E5, D5, C5, B4, A4, B4, C5, E5, D5, B4, C5, A4, rest
            double[] notes = new double[]
            {
                659.25, 587.33, 523.25, 493.88, 440.00, 493.88, 523.25, 659.25,
                587.33, 493.88, 523.25, 440.00, 0,      440.00, 440.00, 0
            };

            int samplesPerNote = (int)(sampleRate * noteDuration);
            int numSamples = samplesPerNote * notes.Length;
            short[] pcm = new short[numSamples];

            for (int n = 0; n < notes.Length; n++)
            {
                double freq = notes[n];
                int startSample = n * samplesPerNote;

                for (int i = 0; i < samplesPerNote; i++)
                {
                    int index = startSample + i;
                    if (freq == 0)
                    {
                        pcm[index] = 0;
                        continue;
                    }

                    double t = (double)i / sampleRate;
                    double angle = 2 * Math.PI * freq * t;
                    
                    // Cute chiptune sound: combination of square wave and triangle wave
                    double square = Math.Sin(angle) >= 0 ? 1.0 : -1.0;
                    double triangle = Math.Abs((angle % (2 * Math.PI)) / Math.PI - 1.0) * 2.0 - 1.0;
                    double mix = (square * 0.7) + (triangle * 0.3);

                    // Linear volume decay per note
                    double env = 1.0 - ((double)i / samplesPerNote);
                    
                    pcm[index] = (short)(5000 * mix * env); // Quiet/harmonic
                }
            }

            byte[] wavBytes = CreateWav(pcm, sampleRate);
            File.WriteAllBytes(filePath, wavBytes);
        }
        catch
        {
            // Fallback
        }

        return filePath;
    }

    private static byte[] CreateWav(short[] pcm, int sampleRate)
    {
        int bytesPerSample = 2; // 16-bit
        int numChannels = 1; // mono
        int subchunk2Size = pcm.Length * bytesPerSample;
        int chunkSize = 36 + subchunk2Size;

        byte[] wav = new byte[44 + subchunk2Size];

        // RIFF chunk descriptor
        Encoding.ASCII.GetBytes("RIFF").CopyTo(wav, 0);
        BitConverter.GetBytes(chunkSize).CopyTo(wav, 4);
        Encoding.ASCII.GetBytes("WAVE").CopyTo(wav, 8);

        // "fmt " sub-chunk
        Encoding.ASCII.GetBytes("fmt ").CopyTo(wav, 12);
        BitConverter.GetBytes(16).CopyTo(wav, 16); // Subchunk1Size
        BitConverter.GetBytes((short)1).CopyTo(wav, 20); // AudioFormat (PCM = 1)
        BitConverter.GetBytes((short)numChannels).CopyTo(wav, 22);
        BitConverter.GetBytes(sampleRate).CopyTo(wav, 24);
        BitConverter.GetBytes(sampleRate * numChannels * bytesPerSample).CopyTo(wav, 28); // ByteRate
        BitConverter.GetBytes((short)(numChannels * bytesPerSample)).CopyTo(wav, 32); // BlockAlign
        BitConverter.GetBytes((short)16).CopyTo(wav, 34); // BitsPerSample

        // "data" sub-chunk
        Encoding.ASCII.GetBytes("data").CopyTo(wav, 36);
        BitConverter.GetBytes(subchunk2Size).CopyTo(wav, 40);

        // Copy PCM data
        Buffer.BlockCopy(pcm, 0, wav, 44, subchunk2Size);

        return wav;
    }
}
