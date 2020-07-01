using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ANYWAYS.UrbanisticPolygons.IO.Download;
using ANYWAYS.UrbanisticPolygons.Logging;
using ANYWAYS.UrbanisticPolygons.Tiles;
using Microsoft.Extensions.Logging;
using OsmSharp;
using OsmSharp.Streams;

namespace ANYWAYS.UrbanisticPolygons.Data
{
    /// <summary>
    /// An OSM tile source.
    /// </summary>
    public class OsmTileSource : IOsmTileSource
    {
        private readonly object Lock = new object();
        private readonly ConcurrentDictionary<uint, uint> _tiles = new ConcurrentDictionary<uint, uint>();
        private readonly CachedDownloader _downloader = new CachedDownloader();
        private readonly ILogger<OsmTileSource> _logger;
        private readonly string _tileUrl;
        private readonly string _cachePath;

        public OsmTileSource(string tileUrl, string cachePath, Logger<OsmTileSource>? logger = null)
        {
            _tileUrl = tileUrl;
            _cachePath = cachePath;
            
            _logger = logger ?? Logger.LoggerFactory.CreateLogger<OsmTileSource>();
        }

        public IEnumerable<OsmGeo> GetTile(uint t)
        {
            // TODO: async this and use task.delay below to prevent thread starvation!
            
            // wait until tile is removed from queue.
            while (true)
            {
                lock (Lock)
                {
                    if (_tiles.ContainsKey(t))
                    {
                        Thread.Sleep(200);
                    }
                    else
                    {
                        _tiles[t] = t;
                        break;
                    }
                }
            }

            try
            {
                var z = 14;
                var (x, y) = TileStatic.ToTile(z, t);
                var tileUrlFormatted = _tileUrl.Replace("{x}", x.ToString())
                    .Replace("{y}", y.ToString())
                    .Replace("{z}", z.ToString());
                var stream = _downloader.Download(tileUrlFormatted, _cachePath);
                if (stream == null) return Enumerable.Empty<OsmGeo>();

                try
                {
                    return (new XmlOsmStreamSource(stream)).ToList();
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Failed to parse tile: {z}{x}/{y}.", 14, x, y);
                    return Enumerable.Empty<OsmGeo>();
                }
            }
            finally
            {
                _tiles.Remove(t, out _);
            }
        }
    }
}