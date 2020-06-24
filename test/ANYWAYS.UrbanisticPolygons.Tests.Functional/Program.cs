using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Faces;
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

            var wechelderzande = (4.801913201808929, 51.26797859372288);
            var staden = (3.0198, 50.9743);
            var leyton = (-0.00303, 51.56436);
            var tile = TileStatic.WorldTileLocalId(staden, 14);

            bool IsBarrier(TagsCollectionBase? tags)
            {
                if (tags == null) return false;

                return DefaultMergeFactorCalculator.Barriers.TryCalculateValue(tags, out _);
            }

            // load data for tile.
            var graph = new TiledBarrierGraph();
            graph.LoadForTile(tile, GetTile, IsBarrier);
            
            // run face assignment for the tile.
            var result =  Graphs.Barrier.Faces.Faces.AssignFaces(graph, tile);
            while (!result.success)
            {
                // extra tiles need loading.
                graph.AddTiles(result.missingTiles, GetTile, IsBarrier);
                
                // try again.
                result =  Graphs.Barrier.Faces.Faces.AssignFaces(graph, tile);
            }
            File.WriteAllText("barriers.geojson", graph.ToFeatures().ToFeatureCollection().ToGeoJson());
        }
    }
}
