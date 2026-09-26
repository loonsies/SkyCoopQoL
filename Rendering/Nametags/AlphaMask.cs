using System;

namespace SkyCoopQoL;

internal static class AlphaMask
{
    // Maximum coverage prevents overlapping strokes from building up opacity.
    internal static byte[] Expand(byte[] source, int width, int height, int radius)
    {
        if (width <= 0 || height <= 0 || source.Length != checked(width * height))
            throw new ArgumentException("Invalid alpha-mask dimensions.");
        if (radius < 0 || radius > 16) throw new ArgumentOutOfRangeException(nameof(radius));
        byte[] result = new byte[source.Length];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                byte coverage = source[y * width + x];
                for (int dy = -radius; dy <= radius && coverage < 255; dy++)
                    for (int dx = -radius; dx <= radius && coverage < 255; dx++)
                    {
                        if (dx * dx + dy * dy > radius * radius) continue;
                        int sx = x + dx, sy = y + dy;
                        if (sx < 0 || sx >= width || sy < 0 || sy >= height) continue;
                        byte sample = source[sy * width + sx];
                        if (sample > coverage) coverage = sample;
                    }
                result[y * width + x] = coverage;
            }
        return result;
    }
}
