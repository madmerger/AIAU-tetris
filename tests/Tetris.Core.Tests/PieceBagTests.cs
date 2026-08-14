using System.Collections.Generic;
using System.Linq;
using Tetris.Core;

namespace Tetris.Core.Tests;

public class PieceBagTests
{
    [Fact]
    public void EachBagContainsAllSevenPiecesWithoutDuplicates()
    {
        var bag = new PieceBag(seed: 42);
        for (int round = 0; round < 20; round++)
        {
            var drawn = Enumerable.Range(0, 7).Select(_ => bag.Next()).ToList();
            Assert.Equal(7, drawn.Distinct().Count());
            Assert.Equal(Tetromino.AllTypes.OrderBy(t => t), drawn.OrderBy(t => t));
        }
    }

    [Fact]
    public void SameSeedProducesSameSequence()
    {
        var a = new PieceBag(seed: 7);
        var b = new PieceBag(seed: 7);
        var first = Enumerable.Range(0, 30).Select(_ => a.Next()).ToList();
        var second = Enumerable.Range(0, 30).Select(_ => b.Next()).ToList();

        Assert.Equal(first, second);
    }

    [Fact]
    public void OrderIsShuffledWithinBag()
    {
        var sequences = new HashSet<string>();
        for (int seed = 0; seed < 20; seed++)
        {
            var bag = new PieceBag(seed);
            sequences.Add(string.Join(",", Enumerable.Range(0, 7).Select(_ => bag.Next())));
        }

        Assert.True(sequences.Count > 1, "バッグ内の並びがシャッフルされていません");
    }
}
