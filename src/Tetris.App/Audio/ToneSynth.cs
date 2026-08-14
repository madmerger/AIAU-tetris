using System;
using System.Collections.Generic;

namespace Tetris.App.Audio;

/// <summary>音源ファイルを持たず、矩形波でメロディ・効果音を合成する。</summary>
public static class ToneSynth
{
    public const int SampleRate = 44100;

    /// <summary>1 音の指定（周波数 Hz、長さ 秒）。周波数 0 は休符。</summary>
    public readonly record struct Note(double Frequency, double Seconds);

    public static float[] Render(IReadOnlyList<Note> notes, double amplitude = 0.22, double dutyCycle = 0.5)
    {
        var total = 0;
        foreach (var note in notes)
        {
            total += (int)(note.Seconds * SampleRate);
        }

        var buffer = new float[total];
        var offset = 0;
        foreach (var note in notes)
        {
            var length = (int)(note.Seconds * SampleRate);
            if (note.Frequency > 0)
            {
                RenderNote(buffer, offset, length, note.Frequency, amplitude, dutyCycle);
            }

            offset += length;
        }

        return buffer;
    }

    private static void RenderNote(
        float[] buffer,
        int offset,
        int length,
        double frequency,
        double amplitude,
        double dutyCycle)
    {
        var period = SampleRate / frequency;
        // クリック音を避けるため、前後 8ms でフェードイン/アウトし、末尾に短い無音を残す。
        var fade = Math.Min((int)(0.008 * SampleRate), length / 4);
        var sustain = (int)(length * 0.88);

        for (var i = 0; i < sustain; i++)
        {
            var phase = (i % period) / period;
            var value = phase < dutyCycle ? amplitude : -amplitude;

            var envelope = 1.0;
            if (i < fade)
            {
                envelope = (double)i / fade;
            }
            else if (i > sustain - fade)
            {
                envelope = Math.Max(0.0, (double)(sustain - i) / fade);
            }

            buffer[offset + i] = (float)(value * envelope);
        }
    }
}

/// <summary>音階の周波数表。</summary>
public static class Pitch
{
    public const double Rest = 0;
    public const double C4 = 261.63;
    public const double D4 = 293.66;
    public const double E4 = 329.63;
    public const double F4 = 349.23;
    public const double G4 = 392.00;
    public const double A4 = 440.00;
    public const double Bb4 = 466.16;
    public const double B4 = 493.88;
    public const double C5 = 523.25;
    public const double D5 = 587.33;
    public const double E5 = 659.25;
    public const double F5 = 698.46;
    public const double G5 = 783.99;
    public const double A5 = 880.00;
    public const double C6 = 1046.50;
}
