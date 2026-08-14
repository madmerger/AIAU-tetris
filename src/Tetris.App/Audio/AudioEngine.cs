using System;
using System.Collections.Generic;
using System.Diagnostics;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Tetris.Core;

namespace Tetris.App.Audio;

/// <summary>効果音の種類（ゲームロジック由来のものに UI 操作音を加えたもの）。</summary>
public enum SoundEffect
{
    MenuMove,
    GameStart,
    PieceLock,
    StageClear,
}

/// <summary>
/// BGM（合成メロディのループ）と効果音を再生する。音声デバイスが利用できない環境では
/// 無効化され、ゲーム進行には影響しない。
/// </summary>
public sealed class AudioEngine : IDisposable
{
    private static readonly WaveFormat Format = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);

    private readonly WaveOutEvent? _output;
    private readonly MixingSampleProvider? _mixer;
    private readonly VolumeSampleProvider? _master;
    private readonly MelodySampleProvider _bgm = new(BuildTheme(), Format);

    private bool _bgmPlaying;
    private bool _muted;
    private bool _bgmSuspended;

    public AudioEngine()
    {
        try
        {
            _mixer = new MixingSampleProvider(Format) { ReadFully = true };
            _master = new VolumeSampleProvider(_mixer) { Volume = 1f };
            _output = new WaveOutEvent { DesiredLatency = 100 };
            _output.Init(_master);
            _output.Play();
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"音声デバイスを初期化できませんでした: {ex.Message}");
            IsAvailable = false;
        }
    }

    public bool IsAvailable { get; }

    /// <summary>ユーザー設定のミュート（BGM・効果音を一括切替）。</summary>
    public bool Muted
    {
        get => _muted;
        set
        {
            _muted = value;
            if (_master is not null)
            {
                _master.Volume = value ? 0f : 1f;
            }
        }
    }

    /// <summary>ポーズによる BGM の一時ミュート（ユーザー設定とは別管理）。</summary>
    public bool BgmSuspended
    {
        get => _bgmSuspended;
        set
        {
            _bgmSuspended = value;
            _bgm.Volume = value ? 0f : 0.18f;
        }
    }

    public void StartBgm()
    {
        if (_mixer is null || _bgmPlaying)
        {
            return;
        }

        _mixer.AddMixerInput(_bgm);
        _bgmPlaying = true;
    }

    public void StopBgm()
    {
        if (_mixer is null || !_bgmPlaying)
        {
            return;
        }

        _mixer.RemoveMixerInput(_bgm);
        _bgmPlaying = false;
    }

    public void Play(SoundEffect effect)
    {
        if (_mixer is null)
        {
            return;
        }

        var tone = effect switch
        {
            SoundEffect.MenuMove => new ToneSampleProvider(Format, 880, 660, TimeSpan.FromSeconds(0.06)),
            SoundEffect.GameStart => new ToneSampleProvider(Format, 440, 880, TimeSpan.FromSeconds(0.18)),
            SoundEffect.PieceLock => new ToneSampleProvider(Format, 220, 150, TimeSpan.FromSeconds(0.06)),
            SoundEffect.StageClear => new ToneSampleProvider(Format, 523, 1046, TimeSpan.FromSeconds(0.4)),
            _ => throw new ArgumentOutOfRangeException(nameof(effect)),
        };

        _mixer.AddMixerInput(tone);
    }

    public void Play(GameSound sound) => Play(sound switch
    {
        GameSound.GameStart => SoundEffect.GameStart,
        GameSound.PieceLock => SoundEffect.PieceLock,
        GameSound.StageClear => SoundEffect.StageClear,
        _ => throw new ArgumentOutOfRangeException(nameof(sound)),
    });

    public void Dispose()
    {
        _output?.Stop();
        _output?.Dispose();
    }

    /// <summary>BGM のメロディ（ロシア民謡「コロブチカ」。パブリックドメイン）。</summary>
    private static List<MelodySampleProvider.Note> BuildTheme()
    {
        const double a4 = 440.00;
        const double b4 = 493.88;
        const double c5 = 523.25;
        const double d5 = 587.33;
        const double e5 = 659.25;
        const double f5 = 698.46;
        const double a5 = 880.00;
        const double gs4 = 415.30;
        var quarter = TimeSpan.FromSeconds(0.32);
        var eighth = TimeSpan.FromSeconds(0.16);
        var half = TimeSpan.FromSeconds(0.64);

        var score = new (double Frequency, TimeSpan Duration)[]
        {
            (e5, quarter), (b4, eighth), (c5, eighth), (d5, quarter), (c5, eighth), (b4, eighth),
            (a4, quarter), (a4, eighth), (c5, eighth), (e5, quarter), (d5, eighth), (c5, eighth),
            (b4, quarter), (b4, eighth), (c5, eighth), (d5, quarter), (e5, quarter),
            (c5, quarter), (a4, quarter), (a4, half),
            (d5, quarter), (f5, eighth), (a5, quarter), (f5, eighth), (e5, eighth),
            (c5, quarter), (e5, eighth), (f5, eighth), (d5, quarter), (c5, eighth), (b4, eighth),
            (b4, quarter), (c5, eighth), (d5, quarter), (e5, quarter),
            (c5, quarter), (a4, quarter), (a4, half),
            (0, eighth), (gs4, quarter), (0, eighth),
        };

        var notes = new List<MelodySampleProvider.Note>(score.Length);
        foreach (var (frequency, duration) in score)
        {
            notes.Add(new MelodySampleProvider.Note(frequency, duration));
        }

        return notes;
    }
}
