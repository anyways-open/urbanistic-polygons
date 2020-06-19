using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
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

            var tile = TileStatic.ToLocalId(
                TileStatic.WorldToTile(4.801913201808929, 51.26797859372288, 14), 14);

            bool IsBarrier(TagsCollectionBase? tags)
            {
                if (tags == null) return false;

                return DefaultMergeFactorCalculator.Barriers.TryCalculateValue(tags, out _);
            }
            
            var barriers = new TiledBarrierGraph();
            barriers.LoadForTile(tile, GetTile, IsBarrier);
            
            var features = new FeatureCollection();
            features.AddRange(barriers.ToFeatures());

            File.WriteAllText("barriers.geojson", features.ToGeoJson());
        }
    }
}
