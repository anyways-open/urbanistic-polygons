using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ANYWAYS.UrbanisticPolygons.Data;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Faces;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Serialization;
using ANYWAYS.UrbanisticPolygons.Graphs.Polygons;
using ANYWAYS.UrbanisticPolygons.Landuse;
using ANYWAYS.UrbanisticPolygons.Tests.Functional.Tests;
using ANYWAYS.UrbanisticPolygons.Tiles;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using Serilog;
using Serilog.Formatting.Json;

namespace ANYWAYS.UrbanisticPolygons.Tests.Functional
{
    class Program
    {
        static void Main(string[] args)
        {
            var logFile = Path.Combine("logs", "log-{Date}.txt");
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .Enrich.FromLogContext()
                .WriteTo.RollingFile(new JsonFormatter(), logFile)
                .WriteTo.Console()
                .CreateLogger();
            
            var cacheFolder = "/media/xivk/2T-SSD-EXT/temp-dev";
            var tileUrl = "https://data1.anyways.eu/tiles/full/20200628-150902/14/{x}/{y}.osm";
            
            var osmTileSource = new OsmTileSource(tileUrl, cacheFolder);

            bool IsBarrier(TagsCollectionBase? tags)
            {
                if (tags == null) return false;

                return DefaultMergeFactorCalculator.Barriers.TryCalculateValue(tags, out _);
            }

            var wechelderzande1 = (4.801913201808929, 51.26797859372288);
            var wechelderzande2 = (4.774868488311768, 51.267366046233136);
            var wechelderzande3 = (4.774868488311768, 51.267366046233136);
            var wechelderzande4 = (4.774868488311768, 51.267366046233136);
            var staden = (3.0198, 50.9743);
            var leyton = (-0.00303, 51.56436);
            var lille = (4.82594, 51.24203);
            var lilleLinksIndustrie = (4.803589582443237, 51.2536864893987);
            var lilleIndustrie = (4.815917015075683, 51.248807861598635);
            var lilleZagerijstraat = (4.8164963722229, 51.233426555935694);
            var lilleHoeksken = (4.826152324676513,
                51.245758876125024);
            var lilleHoeksken1 = (4.826774597167969,
                51.251373150930604);
            var vorselaarSassenhout = (4.807709455490112, 51.21146402264062);
            var vorselaarBeek = (4.7949743270874015, 51.204624839889235);
            var tile1 = TileStatic.WorldTileLocalId(wechelderzande1, 14);
            var tile2 = TileStatic.WorldTileLocalId(wechelderzande2, 14);

            var features1 = BuildFeaturesFor(TileStatic.WorldTileLocalId(lilleHoeksken1, 14),
                osmTileSource, IsBarrier);
            var features2 = BuildFeaturesFor(TileStatic.WorldTileLocalId(lilleHoeksken, 14),
                osmTileSource, IsBarrier);
            
            File.WriteAllText("barriers1.geojson", features1.ToFeatureCollection().ToGeoJson());
            File.WriteAllText("barriers2.geojson", features2.ToFeatureCollection().ToGeoJson());
            return;
            
            var tile = TileStatic.WorldTileLocalId(lilleHoeksken, 14);
            var graph = LoadForTileTest.Default.RunPerformance((tile, osmTileSource, IsBarrier), 1);
            var result = AssignFaceTest.Default.RunPerformance((graph, tile));
            while (!result.success)
            {
                // extra tiles need loading.
                AddTilesTest.Default.RunPerformance((graph, result.missingTiles, osmTileSource, IsBarrier));
                
                // try again.
                result = AssignFaceTest.Default.RunPerformance((graph, tile)); 
            }
            
            // assign landuse.
            //
            // var landuseFeatures = NTSExtensions.FromGeoJson(File.ReadAllText("test.geojson"));
            //
            // IEnumerable<(Polygon polygon, string type)> GetLanduse(((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box)
            // {
            //     return new (Polygon polygon, string type)[] { (landuseFeatures.First().Geometry as Polygon, "residential") };
            // }
            
            IEnumerable<(Polygon polygon, string type)> GetLanduse(
                ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box)
            {
                return LandusePolygons.GetLandusePolygons(box, graph.Zoom, osmTileSource.GetTile, t =>
                {
                    if (DefaultMergeFactorCalculator.Landuses.TryCalculateValue(t, out var type)) return type;
            
                    return null;
                });
            }
            graph.AssignLanduse(tile, GetLanduse); File.WriteAllText("barriers.geojson", graph.ToFeatures().ToFeatureCollection().ToGeoJson()); 
            
            var outerBox = graph.OuterBox(tile);

            // get all landuse polygon in the larger box.
            var landuse = GetLanduse(outerBox).ToList();
                
            File.WriteAllText("landuse.geojson", landuse.Select(x => 
                new Feature(x.polygon, new AttributesTable {{"type", x.type}})).ToFeatureCollection().ToGeoJson());

            // var tile = TileStatic.ToLocalId(8411,5466, 14);
            //
            // // load data for tile.
            // var graph = new TiledBarrierGraph();
            // graph.LoadForTile(tile, osmTileSource.GetTile, IsBarrier);
            //
            // // run face assignment for the tile.
            // var result =  graph.AssignFaces(tile);
            // while (!result.success)
            // {
            //     // extra tiles need loading.-
            //     graph.AddTiles(result.missingTiles, osmTileSource.GetTile, IsBarrier);
            //     
            //     // try again.
            //     result =  graph.AssignFaces(tile);
            // }
            //
            // File.WriteAllText("barriers.geojson", graph.ToFeatures().ToFeatureCollection().ToGeoJson());

            //
            // var landuse = NTSExtensions.FromGeoJson(File.ReadAllText("test.geojson"));
            //
            // IEnumerable<(Polygon polygon, string type)> GetLanduse(((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box)
            // {
            //     return new (Polygon polygon, string type)[] { (landuse.First().Geometry as Polygon, "residential") };
            // }
            // graph.AssignLanduse(tile, GetLanduse);          
            // // File.WriteAllText("barriers.geojson", graph.ToFeatures().ToFeatureCollection().ToGeoJson());

            //
            // TiledBarrierGraphBuilder.BuildForTile(tile, cacheFolder, osmTileSource.GetTile, IsBarrier);
            // //TiledBarrierGraphBuilder.BuildForTile(tile2, "cache", GetTile, IsBarrier);
            //
            // var polygonGraph = new TiledPolygonGraph();
            // polygonGraph.AddTileFromStream(tile,
            //     new GZipStream(File.OpenRead(Path.Combine(cacheFolder, $"{tile1}.tile.graph.zip")),
            //         CompressionMode.Decompress));
            // // polygonGraph.AddTileFromStream(tile2,
            // //      new GZipStream(File.OpenRead(Path.Combine("cache", $"{tile2}.tile.graph.zip")),
            // //          CompressionMode.Decompress));
            //
            // File.WriteAllText("barriers.geojson", polygonGraph.ToFeatures().ToFeatureCollection().ToGeoJson());
        }

        private static IEnumerable<Feature> BuildFeaturesFor(uint tile, OsmTileSource osmTileSource, Func<TagsCollectionBase, bool> isBarrier)
        {
                        var graph = LoadForTileTest.Default.RunPerformance((tile, osmTileSource, isBarrier), 1);
            var result = AssignFaceTest.Default.RunPerformance((graph, tile));
            while (!result.success)
            {
                // extra tiles need loading.
                AddTilesTest.Default.RunPerformance((graph, result.missingTiles, osmTileSource, isBarrier));
                
                // try again.
                result = AssignFaceTest.Default.RunPerformance((graph, tile)); 
            }
            
            // assign landuse.
            //
            // var landuseFeatures = NTSExtensions.FromGeoJson(File.ReadAllText("test.geojson"));
            //
            // IEnumerable<(Polygon polygon, string type)> GetLanduse(((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box)
            // {
            //     return new (Polygon polygon, string type)[] { (landuseFeatures.First().Geometry as Polygon, "residential") };
            // }
            
            IEnumerable<(Polygon polygon, string type)> GetLanduse(
                ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box)
            {
                return LandusePolygons.GetLandusePolygons(box, graph.Zoom, osmTileSource.GetTile, t =>
                {
                    if (DefaultMergeFactorCalculator.Landuses.TryCalculateValue(t, out var type)) return type;
            
                    return null;
                });
            }
            graph.AssignLanduse(tile, GetLanduse);

            return graph.ToFeatures();
        }
    }
}
