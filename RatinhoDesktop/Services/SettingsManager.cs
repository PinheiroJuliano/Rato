using System;
using System.IO;
using System.Text.Json;

namespace RatinhoDesktop.Services;

public class AppSettings
{
    public string PetId { get; set; } = "rato";
    public double Size { get; set; } = 200;
    public bool SqueakEnabled { get; set; } = true;
    public bool MusicEnabled { get; set; } = false;
    public bool AudioReactiveEnabled { get; set; } = false;
    public double Opacity { get; set; } = 1.0;
    public bool Topmost { get; set; } = true;
}

/// <summary>
/// Lê/grava as preferências do usuário em %AppData%\RatinhoDesktop\settings.json,
/// para que o bichinho escolhido e as demais opções sejam lembrados na próxima abertura.
/// </summary>
public static class SettingsManager
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RatinhoDesktop",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null) return settings;
            }
        }
        catch
        {
            // Se o arquivo estiver corrompido ou inacessível, cai para os padrões.
        }

        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            string? dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Falha ao salvar não deve derrubar o app.
        }
    }
}
