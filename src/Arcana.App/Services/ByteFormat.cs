using System.Globalization;

namespace Arcana.App.Services;

public static class ByteFormat
{
    private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB", "PB" };

    public static string Format(long bytes)
    {
        if (bytes < 0)
            return "";

        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        if (unit == 0)
            return $"{bytes} B";

        return value >= 100
            ? $"{value.ToString("0", CultureInfo.InvariantCulture)} {Units[unit]}"
            : $"{value.ToString("0.#", CultureInfo.InvariantCulture)} {Units[unit]}";
    }
}
