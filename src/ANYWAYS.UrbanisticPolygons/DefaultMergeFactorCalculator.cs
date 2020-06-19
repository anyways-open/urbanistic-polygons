using System;
using System.Collections.Generic;
using System.Linq;

namespace ANYWAYS.UrbanisticPolygons
{
    public class DefaultMergeFactorCalculator : IMergeFactorCalculator
    {
        private readonly double _expectedPolygonSize;

        /// <summary>
        /// Barrier resistance for not-similar areas
        /// </summary>
        public static WayWeight<int> Barriers = new WayWeight<int>(
            ("highway", "service", 1),
            ("highway", "pedestrian", 1),
            ("highway", "living_street", 1),
            ("highway", "residential", 2),
            ("highway", "track", 1),
            ("highway", "unclassified", 2),
            ("highway", "tertiary", 4),
            ("highway", "tertiary_link", 4),
            ("highway", "secondary", 8),
            ("highway", "secondary_link", 8),
            ("highway", "primary", 16),
            ("highway", "primary_link", 16),
            ("highway", "motorway", 30),
            ("highway", "motorway_link", 30),
            ("highway", "trunk", 30),
            ("highway", "trunk_link", 30),
            ("railway", "*", 30),
            ("natural", "water", 10)
        );

        public static WayWeight<string> BarrierClassification = new WayWeight<string>(
            ("highway", "service", "residential"),
            ("highway", "pedestrian", "residential"),
            ("highway", "living_street", "residential"),
            ("highway", "residential", "residential"),
            ("highway", "track", "rural"),
            ("highway", "unclassified", "rural"),
            ("railway", "*", "industrial"),
            ("waterway", "*", "water"),
            ("natural", "water", "water")
        );

        public static WayWeight<string> Landuses = new WayWeight<string>(
            ("landuse", "residential", "residential"),
            ("landuse", "industrial", "industrial"),
            ("amenity", "school", "school"),
            ("amenity", "college", "school"),
            ("amenity", "university", "school"),
            ("amenity", "kindergarten", "school"),
            ("landuse", "meadow", "rural"),
            ("landuse", "farmland", "rural"),
            ("landuse", "forest", "natural"),
            ("landuse", "grass", "natural"),
            ("leisure", "park", "natural"),
            ("landuse", "retail", "industrial"),
            ("natural", "water", "water"),
            ("waterway", "riverbank", "water")
        );


        public DefaultMergeFactorCalculator(double expectedPolygonSize)
        {
            _expectedPolygonSize = expectedPolygonSize;
        }

        /// <summary>
        /// Calculates the perimeter of the two polygons before and the new polygon afterwards
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="sharedEdges"></param>
        /// <param name="graph"></param>
        /// <returns></returns>
        public double LengthDifference(UrbanPolygon a, UrbanPolygon b, double sharedLength,
            Graph.Graph graph)
        {
            var aPeri = a.Perimeter(graph);
            var bPeri = b.Perimeter(graph);
            return aPeri + bPeri - (2 * sharedLength);
        }

        private double BarrierMergeProbability(UrbanPolygon a, UrbanPolygon b,
            IEnumerable<(long, long)> sharedEdges, Graph.Graph graph)
        {
            var barrierMergeProbability = 0.0;

            foreach (var edge in sharedEdges)
            {
                var geometry = graph.GetGeometry(edge);
                var l = geometry.Length();

                var aClassification = a.BiggestClassification();
                var bClassification = b.BiggestClassification();
                if (string.IsNullOrEmpty(aClassification) || string.IsNullOrEmpty(bClassification))
                {
                    barrierMergeProbability = 2 * l;
                }
                else if (
                    BarrierClassification.TryCalculateValue(geometry.Tags, out var edgeClass)
                    && aClassification.Equals(edgeClass) && bClassification.Equals(edgeClass))
                {
                    var ratioA = a.GetRatio("_classification:" + edgeClass);
                    var ratioB = b.GetRatio("_classification:" + edgeClass);

                    barrierMergeProbability += l * ratioA * ratioB;
                }
                else
                {
                    var resistance = Barriers.CalculateOrDefault(geometry.Tags, 0);
                    barrierMergeProbability += -l * resistance;
                }
            }


            return barrierMergeProbability;
        }

        /// <summary>
        ///    Euclidian distance between percentual surface usage
        /// E.g. 'a' = 90% residential; 10% natural; 'b' = '95% residential; 5% industrial' 
        /// => sqrt((0.9residential - 0.95residential)² + (0.10 natural - 0.0natural)² + (0.0industrial - 0.05industrial)²)
        /// => sqrt(0.05² + 0.1² + 0.05²)
        /// => 0.12247..
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private double PolygonDifference(UrbanPolygon a, UrbanPolygon b)
        {
            var classifications = a.Tags.Select(t => t.Key).Concat(b.Tags.Select(t => t.Key));
            var difference = 0.0;
            foreach (var key in classifications)
            {
                difference += (a.GetRatio(key) - b.GetRatio(key)) * (a.GetRatio(key) - b.GetRatio(key));
            }

            // Note: value between '0' and 'n', where 'n' is the number of possible classifications. The lower, the more similar

            return difference;
        }

        public double MergeImportance(UrbanPolygon a, UrbanPolygon b,
            IEnumerable<(long, long)> sharedEdges, Graph.Graph graph)
        {
            var sizeDifference = Math.Abs(b.Area - a.Area);


            var diff = PolygonDifference(a, b);
            if (string.IsNullOrEmpty(a.BiggestClassification()) || string.IsNullOrEmpty(b.BiggestClassification()))
            {
                diff = -1;
            }

            var sharedLength = sharedEdges.Sum(id => graph.GetGeometry(id).Length());

            var perimeterDiff = LengthDifference(a, b, sharedLength, graph);


            if (perimeterDiff < 0)
            {
                // The polygons are quite similar and they fuse to something more compact!
                return 1000000;
            }

            // Will be positive if 'a' is smaller then the expected size, 0 otherwise, ratio between 0 - 1
            var smallnessA = Math.Max(0,_expectedPolygonSize -a.Area)/_expectedPolygonSize;
            var smallnessB = Math.Max(0, _expectedPolygonSize - b.Area) / _expectedPolygonSize;

            return 
                (smallnessA * 50) + (smallnessB * 50) +
                (10 - 10 * diff) + (1 - sizeDifference / 10) * BarrierMergeProbability(a, b, sharedEdges, graph);
        }
    }
}