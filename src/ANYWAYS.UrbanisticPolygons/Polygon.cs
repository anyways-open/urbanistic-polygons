using System;
using System.Collections.Generic;
using System.Linq;
using OsmSharp.Complete;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons
{
    public static class PolygonUtils
    {
        public static bool IsClockwise(this CompleteWay way)
        {
            return way.SignedArea() < 0;
        }

        public static void InfuseLanduse(
            this IEnumerable<(CompleteWay, List<(long, long)>)> polygons,
            CompleteWay landusePolygon, string classification)
        {
            if (landusePolygon.IsClockwise())
            {
                Array.Reverse(landusePolygon.Nodes);
            }

            var landuseBBox = new BBox(landusePolygon);

            var key = "_classification:" + classification;

            foreach (var (polygon , _) in polygons)
            {
                Array.Reverse(polygon.Nodes);
                var totalSize = 0.0;

                if (polygon.Tags == null)
                {
                    polygon.Tags = new TagsCollection();
                }

                if (polygon.Tags.TryGetValue(key, out var value))
                {
                    double.TryParse(value, out totalSize);
                }


                var polygonBBox = new BBox(polygon);
                if (!polygonBBox.OverlapsWith(landuseBBox))
                {
                    continue;
                }


                var allWays = new[] {polygon, landusePolygon}.SplitAllWays();

                // We use splitAllWays to intersect the polygon
                // Note that this is invoked by but two lines, which either intersect or do not intersect at all.

                if (allWays.Count == 2)
                {
                    // If there are but two lines, these are the same as the original lines.
                    // Here, three things can happen:
                    // - The landusepolygon is completely contained into the polygon,
                    // - The polygon is completely contained in the landuse polygon
                    // - There is no overlap at all

                    if (landusePolygon.FullyContains(polygon))
                    {
                        totalSize += polygon.Area();
                    }
                    else if (polygon.FullyContains(landusePolygon))
                    {
                        totalSize += landusePolygon.Area();
                    }

                    // No overlap - we don't do anything
                }
                else
                {
                    // There is overlap in the polygon
                    totalSize += SplitWays.IntersectionSurfaceBetween(polygon, landusePolygon);
                }


                polygon.Tags[key] = "" + totalSize;
            }
        }

        private static double SignedArea(this CompleteWay way)
        {
            if (way.Nodes.First() != way.Nodes.Last())
            {
                throw new ArgumentException("First and last node don't match");
            }

            var sum = 0.0;
            for (int i = 1; i < way.Nodes.Length; i++)
            {
                var x1 = way.Nodes[i - 1].Longitude.Value;
                var x2 = way.Nodes[i].Longitude.Value;

                var y1 = way.Nodes[i - 1].Latitude.Value;
                var y2 = way.Nodes[i].Latitude.Value;

                sum += (x2 - x1) * (y2 + y1);
            }

            return sum;
        }

        public static IEnumerable<(long from, long to)> IdPairs(this CompleteWay way)
        {
            for (var i = 1; i < way.Nodes.Length; i++)
            {
                yield return (way.Nodes[i - 1].Id.Value, way.Nodes[i].Id.Value);
            }
        }

        /// <summary>
        /// Splits a way into individual segments
        /// </summary>
        public static IEnumerable<CompleteWay> AsSegments(this CompleteWay way)
        {
            for (var i = 1; i < way.Nodes.Length; i++)
            {
                yield return new CompleteWay()
                {
                    Nodes = new[] {way.Nodes[i - 1], way.Nodes[i]}
                };
            }
        }
    }
}