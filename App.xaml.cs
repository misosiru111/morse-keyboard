using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace MorseKeyboard;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private AppSettings _settings = null!;
    private MorseDecoder _decoder = null!;
    private TreeWindow _window = null!;
    private KeyboardHook? _hook;
    private ToneGenerator? _tone;
    private TrayIcon? _tray;
    private SettingsWindow? _settingsWindow;
    private bool _enabled;

    // Theme artwork: Assets/Themes next to the exe, or the folder given with --assets <dir>.
    // With --assets the folder is watched, so saved edits show up in the running app.
    private string _assetRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "Themes");
    private DispatcherTimer? _assetWatch;
    private string _assetSignature = "";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;

        _singleInstance = new Mutex(true, @"Local\MorseKeyboard.SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            MessageBox.Show("Morse Keyboard はすでに起動しています。タスクトレイのアイコンから操作できます。",
                "Morse Keyboard", MessageBoxButton.OK, MessageBoxImage.Information);
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown();
            return;
        }

        _settings = AppSettings.Load();

        int assetsArg = Array.FindIndex(e.Args, a => a.Equals("--assets", StringComparison.OrdinalIgnoreCase));
        if (assetsArg >= 0 && assetsArg + 1 < e.Args.Length)
        {
            _assetRoot = Path.GetFullPath(e.Args[assetsArg + 1]);
            _assetWatch = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _assetWatch.Tick += (_, _) => ReloadAssetsIfChanged();
        }

        _tone = ToneGenerator.TryCreate();
        ApplySettings();

        _decoder = new MorseDecoder(_settings);
        _assetSignature = ThemeAssets.Signature(AssetDirectory(_settings.ThemeId));
        _window = new TreeWindow(_settings, LoadAssets(_settings.ThemeId));
        _decoder.StateChanged += () => _window.Render(_decoder);
        _decoder.LetterDecoded += OnLetterDecoded;
        _decoder.InvalidCode += code => _window.ShowInvalid(code);
        _decoder.WordGap += OnWordGap;

        _window.ToggleRequested += ToggleEnabled;
        _window.SettingsRequested += ShowSettings;
        _window.HideRequested += () => _window.Hide();

        _tray = new TrayIcon(ToggleEnabled, ToggleWindow, ShowSettings, Shutdown);

        _hook = new KeyboardHook { KeyerVk = _settings.KeyerKey.VirtualKey() };
        // Raised on the hook thread: the tone reacts immediately, decoding happens on the UI thread.
        _hook.KeyPressed += ts =>
        {
            _tone?.SetKey(true);
            Dispatcher.InvokeAsync(() => _decoder.KeyDown(ts), DispatcherPriority.Send);
        };
        _hook.KeyReleased += ts =>
        {
            _tone?.SetKey(false);
            Dispatcher.InvokeAsync(() => _decoder.KeyUp(ts), DispatcherPriority.Send);
        };
        _hook.ToggleRequested += () => Dispatcher.InvokeAsync(ToggleEnabled);

        try
        {
            _hook.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"キーボードフックを設定できませんでした。\n{ex.Message}", "Morse Keyboard",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        SetEnabled(_settings.EnabledOnStartup);
        _window.Render(_decoder);
        _window.Show();
        _assetWatch?.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hook?.Dispose();
        _tone?.Dispose();
        _tray?.Dispose();
        if (_window is not null)
        {
            _window.StoreBounds();
            _settings.Save();
        }
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    private void OnLetterDecoded(char letter, string code)
    {
        char output = _settings.UpperCase ? char.ToUpperInvariant(letter) : char.ToLowerInvariant(letter);
        if (_settings.SendToApp)
            TextSender.Send(output, _settings.SendAsKeystrokes);
        _window.ShowDecoded(letter, code, output);
    }

    private void OnWordGap()
    {
        // In keystroke mode a space would reach the IME as its convert key, so the gap types nothing.
        if (_settings.SendAsKeystrokes) return;
        if (_settings.SendToApp)
            TextSender.Send(' ', _settings.SendAsKeystrokes);
        _window.AppendHistory(' ');
    }

    private void ToggleEnabled() => SetEnabled(!_enabled);

    private void SetEnabled(bool on)
    {
        _enabled = on;
        if (_hook is not null) _hook.Enabled = on;
        if (!on)
        {
            _tone?.SetKey(false);
            _decoder.Reset();
        }
        _window.SetEnabled(on);
        _tray?.SetEnabled(on);
    }

    private void ToggleWindow()
    {
        if (_window.IsVisible) _window.Hide();
        else _window.Show();
    }

    private void ShowSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new SettingsWindow(_settings, ApplySettings);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void ApplySettings()
    {
        _tone?.Configure(_settings.SidetoneEnabled, _settings.SidetoneFrequency, _settings.SidetoneVolume);
        if (_hook is not null) _hook.KeyerVk = _settings.KeyerKey.VirtualKey();
        if (_window is null) return;
        if (_window.ThemeId != _settings.ThemeId)
        {
            _assetSignature = ThemeAssets.Signature(AssetDirectory(_settings.ThemeId));
            _window.ApplyTheme(Themes.Get(_settings.ThemeId), LoadAssets(_settings.ThemeId));
            _window.Render(_decoder);
        }
        _window.ApplySettings();
    }

    private string AssetDirectory(string themeId) => Path.Combine(_assetRoot, themeId);

    private ThemeAssets? LoadAssets(string themeId) => ThemeAssets.Load(AssetDirectory(themeId));

    private void ReloadAssetsIfChanged()
    {
        string signature = ThemeAssets.Signature(AssetDirectory(_settings.ThemeId));
        if (signature == _assetSignature) return;
        _assetSignature = signature;
        _window.ApplyTheme(Themes.Get(_settings.ThemeId), LoadAssets(_settings.ThemeId));
        _window.Render(_decoder);
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MorseKeyboard");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "error.log"), $"[{DateTime.Now:O}] {e.Exception}\n\n");
        }
        catch (Exception)
        {
            // Nothing sensible left to do.
        }
        MessageBox.Show($"エラーが発生しました。\n{e.Exception.Message}", "Morse Keyboard",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
