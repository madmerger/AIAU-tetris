using System;

namespace Tetris.Core;

public static class TimeDisplay
{
    /// <summary>経過時間を `mm:ss:ff`（ff = 1/100 秒）で表す。</summary>
    public static string Format(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
        {
            value = TimeSpan.Zero;
        }

        int minutes = (int)value.TotalMinutes;
        return $"{minutes:00}:{value.Seconds:00}:{value.Milliseconds / 10:00}";
    }
}
