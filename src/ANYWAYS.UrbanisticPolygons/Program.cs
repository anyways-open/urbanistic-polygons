using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OsmSharp.Complete;
using OsmSharp.Streams;

namespace ANYWAYS.UrbanisticPolygons
{
    static class Program
    {
        private static Dictionary<string, double> classificationWeights = new Dictionary<string, double>
        {
            {"school", 2}
        };

        private static Dictionary<string, string> colours = new Dictionary<string, string>
        {
            {"", "#000000"}, // no classification found
            {"residential", "#999900"},
            {"water", "#0000ff"},
            {"rural", "#cc9900"},
            {"natural", "#00ff00"},
            {"school", "#ff3399"},
            {"industrial", "#999999"}
        };

        public static void PostProcess(this CompleteWay geometry)
        {

            if (geometry == null)
            {
                throw new NullReferenceException("geometry");
            }

            var biggestClassification = "";
            var biggestPercentage = 0.0;

            var tags = geometry.Tags;
            var surface = geometry.Area();
            var percentages = new Dictionary<string, string>();
            foreach (var tag in tags)
            {
                if (!tag.Key.StartsWith("_classification:"))
                {
                    continue;
                }

                var classification = tag.Key.Substring("_classification:".Length);

                var ratio = double.Parse(tag.Value) / surface;
                percentages.Add(tag.Key + ":percent", "" + (int) (100 * ratio));

                ratio *= classificationWeights.GetValueOrDefault(classification, 1);

                if (biggestPercentage < ratio)
                {
                    biggestPercentage = ratio;
                    biggestClassification = classification;
                }
            }

            foreach (var (k, v) in percentages)
            {
                tags.Add(k, v);
            }


            tags.Add("fill", colours[biggestClassification]);
            tags.Add("_classification", biggestClassification);
            
        }

        // private static void HandleStream(XmlOsmStreamSource stream, int z, int x, int y)
        // {
        //     var ways = new HashSet<CompleteWay>();
        //     var landusePolygons = new HashSet<CompleteWay>();
        //     foreach (var geo in stream.ToComplete())
        //     {
        //         if (!(geo is CompleteWay w) || w.Tags == null) continue;
        //
        //         if (DefaultMergeFactorCalculator.Barriers.TryCalculateValue(w.Tags, out _))
        //         {
        //             ways.Add(w);
        //         }
        //
        //
        //         if (DefaultMergeFactorCalculator.Landuses.TryCalculateValue(w.Tags, out _))
        //         {
        //             landusePolygons.Add(w);
        //         }
        //     }
        //
        //     var tile = new Tile(x, y, z);
        //     foreach (var w in tile.EdgeWays())
        //     {
        //         ways.Add(w);
        //     }
        //
        //     ways = ways.SplitAllWays();
        //
        //
        //     var graph = new Graph.Graph(ways, tile)
        //         .PruneDeadEnds();
        //
        //     var polygons = graph.GetPolygons();
        //     foreach (var landusePolygon in landusePolygons)
        //     {
        //         DefaultMergeFactorCalculator.Landuses.TryCalculateValue(landusePolygon.Tags, out var classification);
        //         polygons.InfuseLanduse(landusePolygon, classification);
        //     }
        //
        //
        //     File.WriteAllText("barriers.geojson", graph.AsGeoJson());
        //     Console.WriteLine("File written");
        //     File.WriteAllText("polygons.geojson", polygons.Select(p => p.geometry).AsPolygonGeoJson());
        //     Console.WriteLine("Done");
        //
        //
        //     var target = polygons.Average(t => t.geometry.Area());
        //     Console.WriteLine("Target surface is " + target);
        //     var mergeFactor = new DefaultMergeFactorCalculator(target*3.0);
        //     var merger = new PolygonMerger(polygons, graph, mergeFactor, 100);
        //     var merged = merger.MergePolygons().ToList();
        //     merged.ForEach(PostProcess);
        //     File.WriteAllText("polygonsMerged.geojson", merged.AsPolygonGeoJson());
        // }

        static void Main(string[] args)
        {
            // var y = 5469;
            // var x = 8338;
            //
            // using (var f = File.OpenRead($"data/{x}_{y}.osm"))
            // {
            //     var stream = new XmlOsmStreamSource(f);
            //
            //     HandleStream(stream, 14, x, y);
            // }
        }
    }
}