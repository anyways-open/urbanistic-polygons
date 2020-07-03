using System;
using System.Collections.Generic;
using System.IO;

namespace ANYWAYS.UrbanisticPolygons.Tools.OSMCacheBuilder
{
    public static class CachHelpers
    {
        internal static bool TryGetCache(this Dictionary<uint, (Stream stream, long ticks)> cache, uint tile, out Stream stream)
        {
            if (!cache.TryGetValue(tile, out var result))
            {
                stream = null;
                return false;
            }

            cache[tile] = (result.stream, DateTime.Now.Ticks);
            stream = result.stream;
            return true;
        }

        internal static void AddCache(this Dictionary<uint, (Stream stream, long ticks)> cache, uint tile, Stream stream, int max = 256)
        {
            cache[tile] = (stream, DateTime.Now.Ticks);

            while (cache.Count > max)
            {
                var min = uint.MaxValue;
                var ticks = long.MaxValue;
                foreach (var pair in cache)
                {
                    if (pair.Value.ticks >= ticks) continue;

                    ticks = pair.Value.ticks;
                    min = pair.Key;
                }

                cache[min].stream.Flush();
                cache[min].stream.Dispose();
                cache.Remove(min);
            }
        }
    }
}