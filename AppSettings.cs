using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MorseKeyboard;

/// <summary>User settings, persisted as JSON under %APPDATA%\MorseKeyboard.</summary>
public sealed class AppSettings
{
    // Timing. Everything is expressed in dit units so that changing the speed keeps the ratios.
    public int Wpm { get; set; } = 10;
    public double DahThresholdUnits { get; set; } = 2.0;
    public double LetterGapUnits { get; set; } = 3.0;
    public double WordGapUnits { get; set; } = 7.0;
    public bool AutoWordSpace { get; set; } = true;

    // Input and output.
    [JsonConverter(typeof(JsonStringEnumConverter<KeyerKey>))]
    public KeyerKey KeyerKey { get; set; } = KeyerKey.RightCtrl;
    public bool SendToApp { get; set; } = true;
    public bool UpperCase { get; set; }
    public bool SendAsKeystrokes { get; set; }

    // Sidetone.
    public bool SidetoneEnabled { get; set; } = true;
    public double SidetoneFrequency { get; set; } = 650;
    public double SidetoneVolume { get; set; } = 0.3;

    // Window and startup.
    public string ThemeId { get; set; } = Themes.Classic.Id;
    public double WindowOpacity { get; set; } = 0.95;
    public bool EnabledOnStartup { get; set; } = true;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }

    /// <summary>PARIS standard: one dit lasts 1200 / WPM milliseconds.</summary>
    [JsonIgnore] public double UnitMs => 1200.0 / Wpm;
    [JsonIgnore] public double DahThresholdMs => UnitMs * DahThresholdUnits;
    [JsonIgnore] public double LetterGapMs => UnitMs * LetterGapUnits;
    [JsonIgnore] public double WordGapMs => UnitMs * WordGapUnits;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MorseKeyboard", "settings.json");

    public static AppSettings Load()
    {
        AppSettings settings;
        try
        {
            settings = File.Exists(FilePath)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings()
                : new AppSettings();
        }
        catch (Exception)
        {
            settings = new AppSettings();
        }
        settings.Normalize();
        return settings;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception)
        {
            // Losing settings is not worth interrupting the user over.
        }
    }

    /// <summary>Restores every default except the window placement.</summary>
    public void ResetToDefaults()
    {
        var d = new AppSettings();
        Wpm = d.Wpm;
        DahThresholdUnits = d.DahThresholdUnits;
        LetterGapUnits = d.LetterGapUnits;
        WordGapUnits = d.WordGapUnits;
        AutoWordSpace = d.AutoWordSpace;
        KeyerKey = d.KeyerKey;
        SendToApp = d.SendToApp;
        UpperCase = d.UpperCase;
        SendAsKeystrokes = d.SendAsKeystrokes;
        SidetoneEnabled = d.SidetoneEnabled;
        SidetoneFrequency = d.SidetoneFrequency;
        SidetoneVolume = d.SidetoneVolume;
        WindowOpacity = d.WindowOpacity;
        ThemeId = d.ThemeId;
        EnabledOnStartup = d.EnabledOnStartup;
    }

    private void Normalize()
    {
        Wpm = Math.Clamp(Wpm, 5, 40);
        DahThresholdUnits = Math.Clamp(DahThresholdUnits, 1.5, 3.0);
        LetterGapUnits = Math.Clamp(LetterGapUnits, 2.0, 12.0);
        WordGapUnits = Math.Clamp(WordGapUnits, LetterGapUnits + 1, 24.0);
        SidetoneFrequency = Math.Clamp(SidetoneFrequency, 300, 1200);
        SidetoneVolume = Math.Clamp(SidetoneVolume, 0, 1);
        WindowOpacity = Math.Clamp(WindowOpacity, 0.3, 1.0);
        if (!Enum.IsDefined(KeyerKey)) KeyerKey = KeyerKey.RightCtrl;
        ThemeId = Themes.Get(ThemeId).Id;
    }
}
