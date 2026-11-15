using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MorseKeyboard;

/// <summary>
/// Sidetone: a sine wave gated by the key. The output stream runs continuously and the gate only
/// changes the envelope, so the beep starts within one audio buffer of the key press.
/// </summary>
public sealed class ToneGenerator : IDisposable
{
    private readonly KeyedSine _source = new(44100);
    private readonly WaveOutEvent _output;

    private ToneGenerator()
    {
        _output = new WaveOutEvent { DesiredLatency = 50, NumberOfBuffers = 2 };
        _output.Init(new SampleToWaveProvider16(_source));
        _output.Play();
    }

    /// <summary>Returns null when no audio device is available; the app then simply runs silent.</summary>
    public static ToneGenerator? TryCreate()
    {
        try
        {
            return new ToneGenerator();
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Safe to call from any thread.</summary>
    public void SetKey(bool down) => _source.Gate = down;

    public void Configure(bool enabled, double frequency, double volume)
    {
        _source.Enabled = enabled;
        _source.Frequency = frequency;
        _source.Volume = (float)volume;
    }

    public void Dispose()
    {
        _output.Stop();
        _output.Dispose();
    }

    private sealed class KeyedSine : ISampleProvider
    {
        private const float RampSeconds = 0.005f; // short attack/release to avoid clicks

        private readonly int _sampleRate;
        private double _phase;
        private float _envelope;

        public volatile bool Gate;
        public volatile bool Enabled = true;
        public double Frequency = 650;
        public float Volume = 0.3f;

        public KeyedSine(int sampleRate)
        {
            _sampleRate = sampleRate;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            float target = Gate && Enabled ? 1f : 0f;
            float step = 1f / (_sampleRate * RampSeconds);
            double increment = 2 * Math.PI * Frequency / _sampleRate;
            float volume = Volume;

            for (int i = 0; i < count; i++)
            {
                if (_envelope < target) _envelope = Math.Min(target, _envelope + step);
                else if (_envelope > target) _envelope = Math.Max(target, _envelope - step);

                buffer[offset + i] = (float)Math.Sin(_phase) * _envelope * volume;
                _phase += increment;
                if (_phase >= 2 * Math.PI) _phase -= 2 * Math.PI;
            }
            return count;
        }
    }
}
