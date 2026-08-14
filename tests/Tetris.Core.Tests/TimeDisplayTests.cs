using System;
using Tetris.Core;

namespace Tetris.Core.Tests;

public class TimeDisplayTests
{
    [Theory]
    [InlineData(0, "00:00:00")]
    [InlineData(1.23, "00:01:23")]
    [InlineData(61.5, "01:01:50")]
    [InlineData(600, "10:00:00")]
    [InlineData(3600, "60:00:00")]
    public void FormatsAsMinutesSecondsHundredths(double seconds, string expected)
    {
        Assert.Equal(expected, TimeDisplay.Format(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void NegativeValuesAreClampedToZero()
    {
        Assert.Equal("00:00:00", TimeDisplay.Format(TimeSpan.FromSeconds(-5)));
    }
}
