using System.Collections.Generic;

namespace Tetris.App.Audio;

public enum SoundEffect
{
    MenuMove,
    GameStart,
    PieceLock,
    StageClear,
}

/// <summary>BGM と効果音の波形（生成結果はキャッシュ）。</summary>
public static class SoundLibrary
{
    private static readonly Dictionary<SoundEffect, float[]> EffectCache = new();
    private static float[]? _bgmCache;

    /// <summary>BGM: ロシア民謡「行商人（Korobeiniki）」— パブリックドメイン。</summary>
    public static float[] Bgm => _bgmCache ??= ToneSynth.Render(BuildBgm(), amplitude: 0.16, dutyCycle: 0.25);

    public static float[] Effect(SoundEffect effect)
    {
        if (EffectCache.TryGetValue(effect, out var cached))
        {
            return cached;
        }

        var rendered = ToneSynth.Render(BuildEffect(effect), amplitude: 0.25, dutyCycle: 0.5);
        EffectCache[effect] = rendered;
        return rendered;
    }

    private static List<ToneSynth.Note> BuildEffect(SoundEffect effect) => effect switch
    {
        SoundEffect.MenuMove => new List<ToneSynth.Note>
        {
            new(Pitch.A5, 0.055),
        },
        SoundEffect.GameStart => new List<ToneSynth.Note>
        {
            new(Pitch.C5, 0.09),
            new(Pitch.E5, 0.09),
            new(Pitch.G5, 0.09),
            new(Pitch.C6, 0.14),
        },
        SoundEffect.PieceLock => new List<ToneSynth.Note>
        {
            new(Pitch.C4, 0.05),
        },
        SoundEffect.StageClear => new List<ToneSynth.Note>
        {
            new(Pitch.C5, 0.11),
            new(Pitch.D5, 0.11),
            new(Pitch.E5, 0.11),
            new(Pitch.G5, 0.11),
            new(Pitch.C6, 0.28),
        },
        _ => new List<ToneSynth.Note>(),
    };

    private static List<ToneSynth.Note> BuildBgm()
    {
        const double Eighth = 0.19;
        const double Quarter = Eighth * 2;
        const double Dotted = Eighth * 3;
        const double Half = Eighth * 4;

        return new List<ToneSynth.Note>
        {
            new(Pitch.E5, Quarter), new(Pitch.B4, Eighth), new(Pitch.C5, Eighth),
            new(Pitch.D5, Quarter), new(Pitch.C5, Eighth), new(Pitch.B4, Eighth),
            new(Pitch.A4, Quarter), new(Pitch.A4, Eighth), new(Pitch.C5, Eighth),
            new(Pitch.E5, Quarter), new(Pitch.D5, Eighth), new(Pitch.C5, Eighth),
            new(Pitch.B4, Dotted), new(Pitch.C5, Eighth),
            new(Pitch.D5, Quarter), new(Pitch.E5, Quarter),
            new(Pitch.C5, Quarter), new(Pitch.A4, Quarter),
            new(Pitch.A4, Half),

            new(Pitch.Rest, Eighth), new(Pitch.D5, Dotted),
            new(Pitch.F5, Eighth), new(Pitch.A5, Quarter),
            new(Pitch.G5, Eighth), new(Pitch.F5, Eighth),
            new(Pitch.E5, Dotted), new(Pitch.C5, Eighth),
            new(Pitch.E5, Quarter), new(Pitch.D5, Eighth), new(Pitch.C5, Eighth),
            new(Pitch.B4, Quarter), new(Pitch.B4, Eighth), new(Pitch.C5, Eighth),
            new(Pitch.D5, Quarter), new(Pitch.E5, Quarter),
            new(Pitch.C5, Quarter), new(Pitch.A4, Quarter),
            new(Pitch.A4, Half), new(Pitch.Rest, Quarter),
        };
    }
}
