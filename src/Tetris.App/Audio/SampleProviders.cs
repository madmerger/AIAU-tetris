using System;
using NAudio.Wave;

namespace Tetris.App.Audio;

/// <summary>メモリ上の波形を 1 回だけ再生する（効果音用）。</summary>
public sealed class OneShotSampleProvider : ISampleProvider
{
    private readonly float[] _buffer;
    private int _position;

    public OneShotSampleProvider(float[] buffer, WaveFormat waveFormat)
    {
        _buffer = buffer;
        WaveFormat = waveFormat;
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
    {
        var available = Math.Min(count, _buffer.Length - _position);
        Array.Copy(_buffer, _position, buffer, offset, available);
        _position += available;
        return available;
    }
}

/// <summary>メモリ上の波形をループ再生する（BGM 用）。</summary>
public sealed class LoopingSampleProvider : ISampleProvider
{
    private readonly float[] _buffer;
    private int _position;

    public LoopingSampleProvider(float[] buffer, WaveFormat waveFormat)
    {
        if (buffer.Length == 0)
        {
            throw new ArgumentException("空の波形はループできない。", nameof(buffer));
        }

        _buffer = buffer;
        WaveFormat = waveFormat;
    }

    public WaveFormat WaveFormat { get; }

    /// <summary>0.0〜1.0 の出力ゲイン。0 にすると無音のまま再生位置は進む。</summary>
    public float Gain { get; set; } = 1.0f;

    public int Read(float[] buffer, int offset, int count)
    {
        var written = 0;
        while (written < count)
        {
            var chunk = Math.Min(count - written, _buffer.Length - _position);
            for (var i = 0; i < chunk; i++)
            {
                buffer[offset + written + i] = _buffer[_position + i] * Gain;
            }

            _position += chunk;
            written += chunk;
            if (_position >= _buffer.Length)
            {
                _position = 0;
            }
        }

        return count;
    }
}
