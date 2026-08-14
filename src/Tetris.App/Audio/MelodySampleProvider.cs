using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace Tetris.App.Audio;

/// <summary>
/// 矩形波でメロディをループ再生するサンプルプロバイダ（BGM 用）。
/// 音源ファイルを持たず、実行時に波形を合成する。
/// </summary>
public sealed class MelodySampleProvider : ISampleProvider
{
    private readonly IReadOnlyList<Note> _notes;
    private readonly double _sampleRate;

    private int _noteIndex;
    private int _noteSample;

    public MelodySampleProvider(IReadOnlyList<Note> notes, WaveFormat waveFormat)
    {
        _notes = notes;
        WaveFormat = waveFormat;
        _sampleRate = waveFormat.SampleRate;
    }

    public WaveFormat WaveFormat { get; }

    /// <summary>0.0〜1.0 の音量。ポーズ中の一時ミュートに使う。</summary>
    public float Volume { get; set; } = 0.18f;

    public int Read(float[] buffer, int offset, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var note = _notes[_noteIndex];
            int noteLength = (int)(note.Duration.TotalSeconds * _sampleRate);
            double time = _noteSample / _sampleRate;
            float sample = 0f;

            if (note.Frequency > 0)
            {
                // 音の末尾を少し減衰させてクリック音を防ぐ。
                double envelope = Math.Min(1.0, (noteLength - _noteSample) / (_sampleRate * 0.03));
                double phase = time * note.Frequency;
                sample = (float)((phase % 1.0 < 0.5 ? 1.0 : -1.0) * Volume * Math.Max(envelope, 0));
            }

            buffer[offset + i] = sample;

            if (++_noteSample >= noteLength)
            {
                _noteSample = 0;
                _noteIndex = (_noteIndex + 1) % _notes.Count;
            }
        }

        return count;
    }

    public readonly record struct Note(double Frequency, TimeSpan Duration);
}
