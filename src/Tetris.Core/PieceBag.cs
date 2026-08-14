using System;
using System.Collections.Generic;

namespace Tetris.Core;

/// <summary>ピース出現順の供給元。</summary>
public interface IPieceSource
{
    PieceType Next();
}

/// <summary>7 種 1 巡ぶんのバッグからランダムに引く出現順生成器（同一巡内で重複なし）。</summary>
public sealed class PieceBag : IPieceSource
{
    private readonly Random _random;
    private readonly List<PieceType> _remaining = new();

    public PieceBag(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public PieceType Next()
    {
        if (_remaining.Count == 0)
        {
            Refill();
        }

        int index = _random.Next(_remaining.Count);
        var type = _remaining[index];
        _remaining.RemoveAt(index);
        return type;
    }

    private void Refill()
    {
        _remaining.AddRange(Tetromino.AllTypes);
    }
}
