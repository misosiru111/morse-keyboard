using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MorseKeyboard;

/// <summary>Edits <see cref="AppSettings"/> live; every change is applied immediately and saved on close.</summary>
public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly Action _changed;
    private bool _ready;

    public SettingsWindow(AppSettings settings, Action changed)
    {
        _settings = settings;
        _changed = changed;
        InitializeComponent();
        foreach (var key in Enum.GetValues<KeyerKey>())
            KeyerCombo.Items.Add(new ComboBoxItem { Content = key.DisplayName(), Tag = key });
        foreach (var theme in Themes.All)
            ThemeCombo.Items.Add(new ComboBoxItem { Content = theme.Name, Tag = theme.Id });
        LoadValues();
        Closed += (_, _) => _settings.Save();
    }

    private void LoadValues()
    {
        _ready = false;
        WpmSlider.Value = _settings.Wpm;
        DahSlider.Value = _settings.DahThresholdUnits;
        LetterGapSlider.Value = _settings.LetterGapUnits;
        WordGapSlider.Value = _settings.WordGapUnits;
        AutoSpaceCheck.IsChecked = _settings.AutoWordSpace;
        KeyerCombo.SelectedIndex = Array.IndexOf(Enum.GetValues<KeyerKey>(), _settings.KeyerKey);
        SendToAppCheck.IsChecked = _settings.SendToApp;
        UpperCaseCheck.IsChecked = _settings.UpperCase;
        KeystrokeCheck.IsChecked = _settings.SendAsKeystrokes;
        SidetoneCheck.IsChecked = _settings.SidetoneEnabled;
        FrequencySlider.Value = _settings.SidetoneFrequency;
        VolumeSlider.Value = _settings.SidetoneVolume;
        OpacitySlider.Value = _settings.WindowOpacity;
        ThemeCombo.SelectedIndex = Themes.All.ToList().FindIndex(t => t.Id == _settings.ThemeId);
        StartEnabledCheck.IsChecked = _settings.EnabledOnStartup;
        _ready = true;
        UpdateTexts();
    }

    private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => Commit();

    private void OnCheckChanged(object sender, RoutedEventArgs e) => Commit();

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) => Commit();

    private void Commit()
    {
        if (!_ready) return;

        // The word gap must stay longer than the letter gap.
        if (WordGapSlider.Value < LetterGapSlider.Value + 1)
        {
            _ready = false;
            WordGapSlider.Value = LetterGapSlider.Value + 1;
            _ready = true;
        }

        _settings.Wpm = (int)WpmSlider.Value;
        _settings.DahThresholdUnits = DahSlider.Value;
        _settings.LetterGapUnits = LetterGapSlider.Value;
        _settings.WordGapUnits = WordGapSlider.Value;
        _settings.AutoWordSpace = AutoSpaceCheck.IsChecked == true;
        if (KeyerCombo.SelectedItem is ComboBoxItem { Tag: KeyerKey keyer })
            _settings.KeyerKey = keyer;
        _settings.SendToApp = SendToAppCheck.IsChecked == true;
        _settings.UpperCase = UpperCaseCheck.IsChecked == true;
        _settings.SendAsKeystrokes = KeystrokeCheck.IsChecked == true;
        _settings.SidetoneEnabled = SidetoneCheck.IsChecked == true;
        _settings.SidetoneFrequency = FrequencySlider.Value;
        _settings.SidetoneVolume = VolumeSlider.Value;
        _settings.WindowOpacity = OpacitySlider.Value;
        if (ThemeCombo.SelectedItem is ComboBoxItem { Tag: string themeId })
            _settings.ThemeId = themeId;
        _settings.EnabledOnStartup = StartEnabledCheck.IsChecked == true;

        UpdateTexts();
        _changed();
    }

    private void UpdateTexts()
    {
        WpmText.Text = $"{_settings.Wpm} WPM";
        DahText.Text = $"×{_settings.DahThresholdUnits:0.0}";
        LetterGapText.Text = $"×{_settings.LetterGapUnits:0.0}";
        WordGapText.Text = $"×{_settings.WordGapUnits:0.0}";
        FrequencyText.Text = $"{_settings.SidetoneFrequency:0} Hz";
        VolumeText.Text = $"{_settings.SidetoneVolume * 100:0}%";
        OpacityText.Text = $"{_settings.WindowOpacity * 100:0}%";
        AutoSpaceCheck.IsEnabled = !_settings.SendAsKeystrokes;
        TimingSummary.Text =
            $"短点 {_settings.UnitMs:0} ms ／ {_settings.DahThresholdMs:0} ms 以上で長点 ／ " +
            $"{_settings.LetterGapMs:0} ms 離すと文字確定 ／ {_settings.WordGapMs:0} ms 離すと空白";
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _settings.ResetToDefaults();
        LoadValues();
        _changed();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
