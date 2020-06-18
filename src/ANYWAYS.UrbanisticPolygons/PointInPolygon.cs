using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OsmSharp;
using OsmSharp.Complete;

namespace ANYWAYS.UrbanisticPolygons
{
    /// <summary>
    /// An algorithm checking if a point lies within a polygon
    /// Recycled from https://raw.githubusercontent.com/itinero/routing/b2624a27527e0d0d0c357743f5fa3b9872bde235/src/Itinero/Algorithms/Default/PointInPolygon.cs
    /// </summary>
    public static class PointInPolygon
    {

        public static bool FullyContains(this CompleteWay polygon, CompleteWay pointThatShouldBeContained)
        {
            return pointThatShouldBeContained.Nodes.All(polygon.Nodes.PointLiesWithin);
        }
        
        
        /* The basic, actual algorithm
        The algorithm is based on the ray casting algorithm, where the point moves horizontally
        If an even number of intersections are counted, the point lies outside of the polygon
        */
        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        public static bool PointLiesWithin(this Node[] polygon, Node point)
        {
            // no intersections passed yet -> not within the polygon
            bool result = false;


            for (int i = 1; i < polygon.Length; i++)
            {
                // first point == last point
                var start = polygon[i];
                var end = polygon[(i + 1) % polygon.Length];

                var startLongitude = start.Longitude.Value;
                var startLatitude = start.Latitude.Value;
                var pointLongitude = point.Longitude.Value;
                var pointLatitude = point.Latitude.Value;
                var endLongitude = end.Longitude.Value;
                var endLatitude = end.Latitude.Value;

                // The raycast is from west to east - thus at the same latitude level of the point
                // Thus, if the longitude is not between the longitude of the segments, we skip the segment
                // Note that this fails for polygons spanning a pole (e.g. every latitude is 80°, around the world, but the point is at lat. 85°)
                if (!(Math.Min(startLatitude, endLatitude) <= pointLatitude
                      && pointLatitude <= Math.Max(startLatitude, endLatitude)))
                {
                    continue;
                }

                // Here, we know that: the latitude of the point falls between the latitudes of the end points of the segment


                // If both ends of the segment fall to the right, the line will intersect: we toggle our switch and continue with the next segment
                if (Math.Min(startLongitude, endLongitude) >= pointLongitude)
                {
                    result = !result;
                    continue;
                }

                // Analogously, at least one point of the segments should be on the right (east) of the point;
                // otherwise, no intersection is possible (as the raycast goes right)
                if (!(Math.Max(startLongitude, endLongitude) >= point.Longitude))
                {
                    continue;
                }

                // we calculate the longitude on the segment for the latitude of the point
                // x = y_p * (x1 - x2)/(y1 - y2) + (x2y1-x1y1)/(y1-y2)
                var longit = pointLatitude * (startLongitude - endLongitude) + //
                               (endLongitude * startLatitude - startLongitude * endLatitude);
                longit /= (startLatitude - endLatitude);

                // If the longitude lays on the right of the point AND lays within the segment (only right bound is needed to check)
                // the segment intersects the raycast and we flip the bit
                if (longit >= pointLongitude && longit <= Math.Max(startLongitude, endLongitude))
                {
                    result = !result;
                }
            }

            return result;
        }
    }
}