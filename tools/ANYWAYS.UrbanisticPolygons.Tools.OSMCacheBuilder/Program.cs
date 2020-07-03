using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Web;
using Microsoft.Extensions.Configuration;
using OsmSharp;
using OsmSharp.IO.Binary;
using Serilog;

namespace ANYWAYS.UrbanisticPolygons.Tools.OSMCacheBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length < 2)
            {
                args = new[]
                {
                    "/data/work/data/OSM/belgium-latest.osm.pbf",
                    "/media/xivk/2T-SSD-EXT/temp"
                };
            }

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            EnableLogging(config);

            using var stream = File.OpenRead(args[0]);
            var osmPbfStream = new OsmSharp.Streams.PBFOsmStreamSource(stream);
            var osmStream = new OsmSharp.Streams.Filters.OsmStreamFilterProgress();
            osmStream.RegisterSource(osmPbfStream);
            
            var nodeTiles = new TileMap();
            var extraNodeTiles = new TilesMap();
            var wayTiles = new TilesMap();
            
            var tileSet = new HashSet<uint>();
            foreach (var osmGeo in osmStream)
            {
                if (osmGeo is Node node)
                {
                    var tile = TileStatic.WorldTileLocalId(node.Longitude.Value, node.Latitude.Value, 14);
                    nodeTiles.EnsureMinimumSize(node.Id.Value);
                    nodeTiles[node.Id.Value] = tile;
                }
                else if (osmGeo is Way way)
                {
                    tileSet.Clear();
                    foreach (var n in way.Nodes)
                    {
                        if (nodeTiles.Length <= n) continue;
                        var tile = nodeTiles[n];
                        if (tile == 0) continue;

                        tileSet.Add(tile);
                    }

                    wayTiles.Add(way.Id.Value, tileSet);
                    if (tileSet.Count > 1)
                    {
                        var nodeTileSet = new HashSet<uint>();
                        foreach (var n in way.Nodes)
                        {
                            if (nodeTiles.Length <= n) continue;
                            var tile = nodeTiles[n];
                            if (tile == 0) continue;

                            foreach (var otherTile in tileSet)
                            {
                                if (otherTile == tile) continue;

                                nodeTileSet.Add(otherTile);
                            }

                            if (extraNodeTiles.Has(n))
                            {
                                var existingExtraNodes = extraNodeTiles.Get(n);
                                nodeTileSet.UnionWith(existingExtraNodes);
                            }

                            extraNodeTiles.Add(n, nodeTileSet);
                        }
                    }
                }
            }

            var cachePath = args[1];
            string TileFileName(uint tile)
            {
                return Path.Combine(cachePath, $"{tile}.osm.bin");
            }

            foreach (var osmBinFile in Directory.EnumerateFiles(cachePath, "*.osm.bin"))
            {
                File.Delete(osmBinFile);
            }
            
            var tileStreams = new Dictionary<uint, (Stream stream, long ticks)>();
            foreach (var osmGeo in osmStream)
            {
                if (osmGeo.Type == OsmGeoType.Node)
                {
                    var tile = nodeTiles[osmGeo.Id.Value];
                    if (!tileStreams.TryGetCache(tile, out var tileStream))
                    {
                        tileStream = File.Open(TileFileName(tile), FileMode.Append);
                        tileStreams.AddCache(tile, tileStream);
                    }
                    
                    tileStream.Append(osmGeo);

                    foreach (var otherTile in extraNodeTiles.Get(osmGeo.Id.Value))
                    {
                        if (!tileStreams.TryGetCache(otherTile, out tileStream))
                        {
                            tileStream = File.Open(TileFileName(otherTile), FileMode.Append);
                            tileStreams.AddCache(otherTile, tileStream);
                        }
                    
                        tileStream.Append(osmGeo);
                    }
                }
                else
                {
                    foreach (var tile in wayTiles.Get(osmGeo.Id.Value))
                    {
                        if (!tileStreams.TryGetCache(tile, out var tileStream))
                        {
                            tileStream = File.Open(TileFileName(tile), FileMode.Append);
                            tileStreams.AddCache(tile, tileStream);
                        }
                    
                        tileStream.Append(osmGeo);
                    }
                }
            }

            foreach (var pair in tileStreams)
            {
                pair.Value.stream.Flush();
                pair.Value.stream.Dispose();
            }

            var url = "https://data1.anyways.eu/tiles/full/20200628-150902/{z}/{x}/{y}.osm";
            var baseFileName = 
                url.Replace('/', '-')
                    .Replace(':', '-');
            var gzipTileName = Path.Combine(cachePath, baseFileName + ".tile.zip");
            foreach (var osmBinFile in Directory.EnumerateFiles(cachePath, "*.osm.bin"))
            {
                var file = new FileInfo(osmBinFile);
                var fileName = file.Name.Substring(0, file.Name.IndexOf('.'));
                if (!uint.TryParse(fileName, NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture,
                    out var tile)) continue;
                
                Log.Information($"Converting {osmBinFile}...");

                using (var inputStream = File.OpenRead(osmBinFile))
                {
                    var binarySource = new OsmSharp.Streams.BinaryOsmStreamSource(inputStream);
                    var fullTile = TileStatic.ToTile(14, tile);
                    var tileGzipName = gzipTileName.Replace("{x}", fullTile.x.ToString())
                        .Replace("{y}", fullTile.y.ToString()).Replace("{z}", "14");
                    using (var outputStream = File.Open(tileGzipName, FileMode.Create))
                    using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Fastest))
                    {
                        var xmlTarget = new OsmSharp.Streams.XmlOsmStreamTarget(gzipStream);
                        xmlTarget.RegisterSource(binarySource);
                        xmlTarget.Pull();
                    }
                }
                    
                File.Delete(osmBinFile);
            }
        }
        
        private static void EnableLogging(IConfigurationRoot config)
        {
            // enable logging.
            OsmSharp.Logging.Logger.LogAction = (origin, level, message, parameters) =>
            {
                var formattedMessage = $"{origin} - {message}";
                switch (level)
                {
                    case "critical":
                        Log.Fatal(formattedMessage);
                        break;
                    case "error":
                        Log.Error(formattedMessage);
                        break;
                    case "warning":
                        Log.Warning(formattedMessage);
                        break;
                    case "verbose":
                        Log.Verbose(formattedMessage);
                        break;
                    case "information":
                        Log.Information(formattedMessage);
                        break;
                    default:
                        Log.Debug(formattedMessage);
                        break;
                }
            };


            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();
        }
    }
}
