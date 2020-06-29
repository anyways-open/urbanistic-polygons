using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Faces;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Serialization;
using ANYWAYS.UrbanisticPolygons.Graphs.Polygons;
using ANYWAYS.UrbanisticPolygons.Landuse;
using ANYWAYS.UrbanisticPolygons.Tests.Functional.Download;
using ANYWAYS.UrbanisticPolygons.Tiles;
using NetTopologySuite.Features;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using Serilog;

namespace ANYWAYS.UrbanisticPolygons.Tests.Functional
{
    class Program
    {
        static void Main(string[] args)
        {
            IEnumerable<OsmGeo> GetTile(uint t)
            {
                var (x, y) = TileStatic.ToTile(14, t);
                var stream = DownloadHelper.Download($"https://data1.anyways.eu/tiles/debug/{14}/{x}/{y}.osm");
                if (stream == null) return Enumerable.Empty<OsmGeo>();

                try
                {
                    return (new XmlOsmStreamSource(stream)).ToList();
                }
                catch (Exception e)
                {
                    Log.Warning($"Failed to parse tile: {14}{x}/{y}");
                    return Enumerable.Empty<OsmGeo>();
                }
            }

            var wechelderzande1 = (4.801913201808929, 51.26797859372288);
            var wechelderzande2 = (4.774868488311768, 51.267366046233136);
            var wechelderzande3 = (4.774868488311768, 51.267366046233136);
            var wechelderzande4 = (4.774868488311768, 51.267366046233136);
            var staden = (3.0198, 50.9743);
            var leyton = (-0.00303, 51.56436);
            var tile1 = TileStatic.WorldTileLocalId(wechelderzande1, 14);
            var tile2 = TileStatic.WorldTileLocalId(wechelderzande2, 14);

            bool IsBarrier(TagsCollectionBase? tags)
            {
                if (tags == null) return false;

                return DefaultMergeFactorCalculator.Barriers.TryCalculateValue(tags, out _);
            }
            
            TiledBarrierGraphBuilder.BuildForTile(tile1, "cache", GetTile, IsBarrier);
            TiledBarrierGraphBuilder.BuildForTile(tile2, "cache", GetTile, IsBarrier);
            
            var polygonGraph = new TiledPolygonGraph();
            polygonGraph.AddTileFromStream(tile1,
                new GZipStream(File.OpenRead(Path.Combine("cache", $"{tile1}.tile.graph.zip")),
                    CompressionMode.Decompress));
            polygonGraph.AddTileFromStream(tile2,
                 new GZipStream(File.OpenRead(Path.Combine("cache", $"{tile2}.tile.graph.zip")),
                     CompressionMode.Decompress));
            
            File.WriteAllText("barriers.geojson", polygonGraph.ToFeatures().ToFeatureCollection().ToGeoJson());
        }
    }
}
