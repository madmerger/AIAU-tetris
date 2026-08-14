using System.Collections.Generic;
using Tetris.Core;

namespace Tetris.Core.Tests;

/// <summary>テスト用に出現順を固定する供給元。列挙し終えた後は最後のピースを繰り返す。</summary>
internal sealed class FixedPieceSource : IPieceSource
{
    private readonly IReadOnlyList<PieceType> _sequence;
    private int _index;

    public FixedPieceSource(params PieceType[] sequence)
    {
        _sequence = sequence;
    }

    public PieceType Next()
    {
        var type = _sequence[System.Math.Min(_index, _sequence.Count - 1)];
        _index++;
        return type;
    }
}
