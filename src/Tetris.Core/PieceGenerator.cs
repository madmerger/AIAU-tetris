namespace Tetris.Core;

public interface IPieceGenerator
{
    PieceType Next();
}

/// <summary>7 種 1 巡のバッグからランダムに引く（同一巡内で重複しない）。</summary>
public sealed class BagPieceGenerator : IPieceGenerator
{
    private readonly Random _random;
    private readonly List<PieceType> _bag = new();

    public BagPieceGenerator(Random? random = null)
    {
        _random = random ?? new Random();
    }

    public PieceType Next()
    {
        if (_bag.Count == 0)
        {
            Refill();
        }

        var index = _random.Next(_bag.Count);
        var type = _bag[index];
        _bag.RemoveAt(index);
        return type;
    }

    private void Refill() => _bag.AddRange(Tetromino.AllTypes);
}

/// <summary>テスト用に決まった順序でピースを返す。</summary>
public sealed class FixedPieceGenerator : IPieceGenerator
{
    private readonly IReadOnlyList<PieceType> _sequence;
    private int _index;

    public FixedPieceGenerator(params PieceType[] sequence)
    {
        if (sequence.Length == 0)
        {
            throw new ArgumentException("1 つ以上のピースが必要。", nameof(sequence));
        }

        _sequence = sequence;
    }

    public PieceType Next()
    {
        var type = _sequence[_index % _sequence.Count];
        _index++;
        return type;
    }
}
