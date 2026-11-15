using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Threading;

namespace MorseKeyboard;

/// <summary>
/// Turns key press/release timestamps into dits, dahs, letters and word gaps.
/// Lives on the UI thread; timestamps come from <see cref="Stopwatch.GetTimestamp"/>.
/// </summary>
public sealed class MorseDecoder
{
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _timer;
    private readonly StringBuilder _code = new();
    private long _pressedAt;
    private long _releasedAt;
    private bool _spaceDue; // a letter was decoded since the last word gap

    public MorseDecoder(AppSettings settings)
    {
        _settings = settings;
        _timer = new DispatcherTimer(DispatcherPriority.Send);
        _timer.Tick += (_, _) => OnTimer();
    }

    /// <summary>Elements entered so far for the current letter.</summary>
    public string Code => _code.ToString();

    public bool IsKeyDown { get; private set; }

    /// <summary>While the key is held: true once the press has lasted long enough to count as a dah.</summary>
    public bool IsDahPending { get; private set; }

    public event Action? StateChanged;
    public event Action<char, string>? LetterDecoded;
    public event Action<string>? InvalidCode;
    public event Action? WordGap;

    public void KeyDown(long timestamp)
    {
        if (IsKeyDown) return;
        FlushGaps(timestamp);
        IsKeyDown = true;
        IsDahPending = false;
        _pressedAt = timestamp;
        ScheduleAfter(_pressedAt, _settings.DahThresholdMs);
        StateChanged?.Invoke();
    }

    public void KeyUp(long timestamp)
    {
        if (!IsKeyDown) return;
        IsKeyDown = false;
        IsDahPending = false;
        _releasedAt = timestamp;
        _code.Append(ElapsedMs(_pressedAt, timestamp) >= _settings.DahThresholdMs ? '-' : '.');
        ScheduleAfter(_releasedAt, _settings.LetterGapMs);
        StateChanged?.Invoke();
    }

    public void Reset()
    {
        _timer.Stop();
        _code.Clear();
        IsKeyDown = false;
        IsDahPending = false;
        _spaceDue = false;
        StateChanged?.Invoke();
    }

    private void OnTimer()
    {
        _timer.Stop();
        long now = Stopwatch.GetTimestamp();

        if (IsKeyDown)
        {
            if (ElapsedMs(_pressedAt, now) >= _settings.DahThresholdMs)
            {
                IsDahPending = true;
                StateChanged?.Invoke();
            }
            else
            {
                ScheduleAfter(_pressedAt, _settings.DahThresholdMs);
            }
            return;
        }

        FlushGaps(now);
        if (_code.Length > 0)
            ScheduleAfter(_releasedAt, _settings.LetterGapMs);
        else if (_spaceDue)
            ScheduleAfter(_releasedAt, _settings.WordGapMs);
    }

    /// <summary>
    /// Ends the letter and/or the word if enough silence has passed by <paramref name="now"/>.
    /// Also called on key down, so a press that races the timer still closes the previous letter first.
    /// </summary>
    private void FlushGaps(long now)
    {
        double silence = ElapsedMs(_releasedAt, now);

        if (_code.Length > 0 && silence >= _settings.LetterGapMs)
            CommitLetter();

        if (_code.Length == 0 && _spaceDue && silence >= _settings.WordGapMs)
        {
            _spaceDue = false;
            if (_settings.AutoWordSpace) WordGap?.Invoke();
        }
    }

    private void CommitLetter()
    {
        string code = _code.ToString();
        _code.Clear();
        if (MorseTable.TryDecode(code, out char c))
        {
            _spaceDue = true;
            LetterDecoded?.Invoke(c, code);
        }
        else
        {
            InvalidCode?.Invoke(code);
        }
        StateChanged?.Invoke();
    }

    private void ScheduleAfter(long from, double delayMs)
    {
        double remaining = delayMs - ElapsedMs(from, Stopwatch.GetTimestamp());
        _timer.Stop();
        _timer.Interval = TimeSpan.FromMilliseconds(Math.Max(1, remaining));
        _timer.Start();
    }

    private static double ElapsedMs(long from, long to) => (to - from) * 1000.0 / Stopwatch.Frequency;
}
