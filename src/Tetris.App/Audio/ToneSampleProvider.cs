using System;
using NAudio.Wave;

namespace Tetris.App.Audio;

/// <summary>短い矩形波トーン（効果音）。再生し切ると終了する。</summary>
public sealed class ToneSampleProvider : ISampleProvider
{
    private readonly double _startFrequency;
    private readonly double _endFrequency;
    private readonly float _amplitude;
    private readonly int _totalSamples;

    private int _position;
    private double _phase;

    public ToneSampleProvider(
        WaveFormat waveFormat,
        double startFrequency,
        double endFrequency,
        TimeSpan duration,
        float amplitude = 0.22f)
    {
        WaveFormat = waveFormat;
        _startFrequency = startFrequency;
        _endFrequency = endFrequency;
        _amplitude = amplitude;
        _totalSamples = (int)(duration.TotalSeconds * waveFormat.SampleRate);
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
    {
        int written = 0;
        while (written < count && _position < _totalSamples)
        {
            double progress = (double)_position / _totalSamples;
            double frequency = _startFrequency + ((_endFrequency - _startFrequency) * progress);
            _phase += frequency / WaveFormat.SampleRate;
            double envelope = 1.0 - progress;
            buffer[offset + written] = (float)((_phase % 1.0 < 0.5 ? 1.0 : -1.0) * _amplitude * envelope);
            written++;
            _position++;
        }

        return written;
    }
}
