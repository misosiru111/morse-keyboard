using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using static MorseKeyboard.NativeMethods;

namespace MorseKeyboard;

/// <summary>
/// Global low-level keyboard hook. While enabled it takes the keyer key for itself (for the space bar,
/// only when no modifier is held) and reports press/release timestamps. Also watches for the Ctrl+Alt+M toggle.
/// Runs on its own thread so UI work can never delay the hook; events are raised on that thread.
/// </summary>
public sealed class KeyboardHook : IDisposable
{
    private const int ToggleKey = 0x4D; // M, together with Ctrl+Alt

    private readonly LowLevelKeyboardProc _proc; // kept alive for the lifetime of the hook
    private Thread? _thread;
    private uint _threadId;
    private IntPtr _hook;
    private volatile bool _enabled;
    private volatile int _keyerVk = VK_RCONTROL;

    // Hook thread only. The key whose press we swallowed, so its release is swallowed too
    // even if the keyer key or the enabled state changed in between.
    private int _heldVk;
    private bool _toggleDown;

    public KeyboardHook() => _proc = HookProc;

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    /// <summary>Virtual-key code of the keyer key.</summary>
    public int KeyerVk
    {
        get => _keyerVk;
        set => _keyerVk = value;
    }

    /// <summary>Keyer key pressed; argument is a <see cref="Stopwatch"/> timestamp.</summary>
    public event Action<long>? KeyPressed;

    /// <summary>Keyer key released; argument is a <see cref="Stopwatch"/> timestamp.</summary>
    public event Action<long>? KeyReleased;

    public event Action? ToggleRequested;

    public void Start()
    {
        using var ready = new ManualResetEventSlim();
        Exception? error = null;

        _thread = new Thread(() =>
        {
            _threadId = GetCurrentThreadId();
            _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
            if (_hook == IntPtr.Zero)
                error = new Win32Exception(Marshal.GetLastWin32Error());
            ready.Set();
            if (_hook == IntPtr.Zero) return;

            while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
            UnhookWindowsHookEx(_hook);
        })
        {
            IsBackground = true,
            Name = "MorseKeyboard hook",
            Priority = ThreadPriority.AboveNormal,
        };
        _thread.Start();
        ready.Wait();
        if (error is not null) throw error;
    }

    public void Dispose()
    {
        if (_threadId != 0)
            PostThreadMessage(_threadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        _thread?.Join(1000);
    }

    private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                var info = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                // Injected input includes our own SendInput output; never touch it.
                if ((info.flags & LLKHF_INJECTED) == 0 && Handle((int)info.vkCode, (int)wParam))
                    return (IntPtr)1;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    /// <returns>True to swallow the key event.</returns>
    private bool Handle(int vk, int message)
    {
        bool down = message is WM_KEYDOWN or WM_SYSKEYDOWN;

        if (vk == ToggleKey)
        {
            if (down && IsPressed(VK_CONTROL) && IsPressed(VK_MENU))
            {
                if (!_toggleDown)
                {
                    _toggleDown = true;
                    ToggleRequested?.Invoke();
                }
                return true;
            }
            if (!down && _toggleDown)
            {
                _toggleDown = false;
                return true;
            }
            return false;
        }

        if (down)
        {
            if (vk == _heldVk) return true; // auto-repeat while held
            if (_heldVk != 0 || vk != _keyerVk || !_enabled) return false;
            // Shift/Ctrl/Alt + Space stays an ordinary space; other keyer keys are always taken.
            if (vk == VK_SPACE && ModifierHeld()) return false;
            _heldVk = vk;
            KeyPressed?.Invoke(Stopwatch.GetTimestamp());
            return true;
        }

        if (vk != _heldVk) return false;
        _heldVk = 0;
        KeyReleased?.Invoke(Stopwatch.GetTimestamp());
        return true;
    }

    private static bool ModifierHeld() =>
        IsPressed(VK_SHIFT) || IsPressed(VK_CONTROL) || IsPressed(VK_MENU) || IsPressed(VK_LWIN) || IsPressed(VK_RWIN);

    private static bool IsPressed(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;
}
