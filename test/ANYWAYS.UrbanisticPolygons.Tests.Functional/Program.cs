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
using NetTopologySuite.Geometries;
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
            var cacheFolder = "/media/xivk/2T-SSD-EXT/temp";
            
            IEnumerable<OsmGeo> GetTile(uint t)
            {
                var (x, y) = TileStatic.ToTile(14, t);
                var stream = DownloadHelper.Download($"https://data1.anyways.eu/tiles/full/20200628-150902/{14}/{x}/{y}.osm",
                    cacheFolder);
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

            var tile = TileStatic.ToLocalId(8411,5466, 14);
            
            // load data for tile.
            var graph = new TiledBarrierGraph();
            graph.LoadForTile(tile, GetTile, IsBarrier);
            
            // run face assignment for the tile.
            var result =  graph.AssignFaces(tile);
            while (!result.success)
            {
                // extra tiles need loading.-
                graph.AddTiles(result.missingTiles, GetTile, IsBarrier);
                
                // try again.
                result =  graph.AssignFaces(tile);
            }
            
            File.WriteAllText("barriers.geojson", graph.ToFeatures().ToFeatureCollection().ToGeoJson());
            
            //
            // var landuse = NTSExtensions.FromGeoJson(File.ReadAllText("test.geojson"));
            //
            // IEnumerable<(Polygon polygon, string type)> GetLanduse(((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box)
            // {
            //     return new (Polygon polygon, string type)[] { (landuse.First().Geometry as Polygon, "residential") };
            // }
            // graph.AssignLanduse(tile, GetLanduse);          
            // // File.WriteAllText("barriers.geojson", graph.ToFeatures().ToFeatureCollection().ToGeoJson());

            
            TiledBarrierGraphBuilder.BuildForTile(tile, cacheFolder, GetTile, IsBarrier);
            //TiledBarrierGraphBuilder.BuildForTile(tile2, "cache", GetTile, IsBarrier);
            
            var polygonGraph = new TiledPolygonGraph();
            polygonGraph.AddTileFromStream(tile,
                new GZipStream(File.OpenRead(Path.Combine(cacheFolder, $"{tile1}.tile.graph.zip")),
                    CompressionMode.Decompress));
            // polygonGraph.AddTileFromStream(tile2,
            //      new GZipStream(File.OpenRead(Path.Combine("cache", $"{tile2}.tile.graph.zip")),
            //          CompressionMode.Decompress));
            
            File.WriteAllText("barriers.geojson", polygonGraph.ToFeatures().ToFeatureCollection().ToGeoJson());
        }
    }
}
