using System;
using System.Diagnostics;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Tetris.App.Audio;

/// <summary>BGM ループと効果音の同時再生を担う。音声デバイスが無い環境では自動的に無効化される。</summary>
public sealed class AudioEngine : IDisposable
{
    private static readonly WaveFormat Format = WaveFormat.CreateIeeeFloatWaveFormat(ToneSynth.SampleRate, 1);

    private readonly IWavePlayer? _output;
    private readonly MixingSampleProvider? _mixer;
    private LoopingSampleProvider? _bgm;
    private bool _muted;
    private bool _pauseMuted;

    public AudioEngine()
    {
        try
        {
            _mixer = new MixingSampleProvider(Format) { ReadFully = true };
            _output = new WaveOutEvent { DesiredLatency = 120 };
            _output.Init(_mixer);
            _output.Play();
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            // 音声デバイスが無い環境（CI・リモートセッション等）では無音で続行する。
            Debug.WriteLine($"音声出力を初期化できなかったため無音で動作する: {ex.Message}");
            _output?.Dispose();
            _output = null;
            _mixer = null;
            IsAvailable = false;
        }
    }

    public bool IsAvailable { get; }

    /// <summary>ユーザーのミュート設定（BGM・効果音を一括切替）。</summary>
    public bool Muted
    {
        get => _muted;
        set
        {
            _muted = value;
            ApplyBgmGain();
        }
    }

    /// <summary>ポーズによる一時ミュート。ユーザー設定とは別管理。</summary>
    public bool PauseMuted
    {
        get => _pauseMuted;
        set
        {
            _pauseMuted = value;
            ApplyBgmGain();
        }
    }

    public void StartBgm()
    {
        if (_mixer is null || _bgm is not null)
        {
            return;
        }

        _bgm = new LoopingSampleProvider(SoundLibrary.Bgm, Format);
        ApplyBgmGain();
        _mixer.AddMixerInput(_bgm);
    }

    public void StopBgm()
    {
        if (_mixer is null || _bgm is null)
        {
            return;
        }

        _mixer.RemoveMixerInput(_bgm);
        _bgm = null;
    }

    public void Play(SoundEffect effect)
    {
        if (_mixer is null || _muted)
        {
            return;
        }

        _mixer.AddMixerInput(new OneShotSampleProvider(SoundLibrary.Effect(effect), Format));
    }

    public void Dispose()
    {
        StopBgm();
        _output?.Stop();
        _output?.Dispose();
    }

    private void ApplyBgmGain()
    {
        if (_bgm is not null)
        {
            _bgm.Gain = _muted || _pauseMuted ? 0f : 1f;
        }
    }
}
