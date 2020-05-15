using System;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using OsmSharp;
using OsmSharp.Complete;

namespace ANYWAYS.UrbanisticPolygons
{
    public static class SplitWays
    {
        public static Coordinate Coordinate(this Node n)
        {
            return new Coordinate(n.Longitude.Value, n.Latitude.Value);
        }



        private const double TOLERANCE = 0.0000001;

        /// <summary>
        /// IF the way 'toSplit' intersects with 'splitter', then the first parts are returned as 'leftPart', the rest as 'rightPart'
        /// Note that both leftPart and rightPart might still need a split, though the chance of having to split leftPart again is small
        /// </summary>
        /// <param name="toSplit"></param>
        /// <param name="splitter"></param>
        /// <returns></returns>
        private static (CompleteWay leftPart, CompleteWay rightPart) SplitWay(CompleteWay toSplit, CompleteWay splitter)
        {

            for (int i = 1; i < toSplit.Nodes.Length; i++)
            {
                var p1 = toSplit.Nodes[i - 1].Coordinate();
                var p2 = toSplit.Nodes[i].Coordinate();


                for (int j = 0; j < splitter.Nodes.Length; j++)
                {
                    if (toSplit.Nodes[i].Id.Equals(splitter.Nodes[j].Id)
                        && i != 0 && i != toSplit.Nodes.Length - 1)
                    {
                        // THis point is shared by both ways
                        // We have to split at 'i' which is still shared between the two parts
                        var leftPrt = new CompleteWay
                        {
                            Tags = toSplit.Tags,
                            Nodes = toSplit.Nodes.SubArray(0, i + 1)
                        };
                        var rightPrt = new CompleteWay
                        {
                            Tags = toSplit.Tags,
                            Nodes = toSplit.Nodes.SubArray(i, toSplit.Nodes.Length)
                        };
                        return (leftPrt, rightPrt);
                    }


                    if (j == 0)
                    {
                        continue;
                    }

                    var q1 = splitter.Nodes[j - 1].Coordinate();
                    var q2 = splitter.Nodes[j].Coordinate();


                    var intersector = new RobustLineIntersector();
                    intersector.ComputeIntersection(p1, p2, q1, q2);

                    if (!intersector.HasIntersection)
                    {
                        continue;
                    }

                    if (!intersector.IsProper)
                    {
                        continue;
                    }

                    if (!intersector.IsInteriorIntersection())
                    {
                        continue;
                    }

                    var intersectionPoint = intersector.GetIntersection(0);
                    var newPoint = new Node
                    {
                        Latitude = intersectionPoint.Y,
                        Longitude = intersectionPoint.X
                    };
                    if (Math.Abs(toSplit.Nodes[i].Latitude.Value - newPoint.Latitude.Value) < TOLERANCE
                        && Math.Abs(toSplit.Nodes[i].Longitude.Value - newPoint.Longitude.Value) < TOLERANCE)
                    {
                        // Not a proper intersection: right line will be a dot
                        continue;
                    }

                    if (Math.Abs(toSplit.Nodes[i - 1].Latitude.Value - newPoint.Latitude.Value) < TOLERANCE
                        && Math.Abs(toSplit.Nodes[i - 1].Longitude.Value - newPoint.Longitude.Value) < TOLERANCE)
                    {
                        // Not a proper intersection: left line will be a dot
                        continue;
                    }

                    var leftPart = new CompleteWay
                    {
                        Tags = toSplit.Tags,
                        Nodes = toSplit.Nodes.SubArray(0, i + 1)
                    };
                    var rightPart = new CompleteWay
                    {
                        Tags = toSplit.Tags,
                        Nodes = toSplit.Nodes.SubArray(i - 1, toSplit.Nodes.Length)
                    };


                    leftPart.Nodes[i] = newPoint;
                    rightPart.Nodes[0] = newPoint;

                    return (leftPart, rightPart);
                }
            }

            return (toSplit, null);
        }


        private static void FullSplit(CompleteWay toSplit, BBox splitBbox,
            CompleteWay splitter, BBox splitterBbox,
            ISet<(CompleteWay, BBox)> newSegments)
        {
            if (!splitterBbox.OverlapsWith(splitBbox))
            {
                newSegments.Add((toSplit, splitBbox));
                return;
            }

            var (left, right) = SplitWay(toSplit, splitter);
            if (right == null)
            {
                newSegments.Add((left, splitBbox));
                return;
            }

            FullSplit(left, new BBox(left), splitter, splitterBbox, newSegments);
            FullSplit(right, new BBox(right), splitter, splitterBbox, newSegments);
        }

        public static HashSet<CompleteWay> SplitAllWays(this IEnumerable<CompleteWay> allWays)
        {
            var withBBox = allWays.Select(w => (w, new BBox(w))).ToList();

            var allSegments = new HashSet<CompleteWay>();


            var newWaySegments = new HashSet<(CompleteWay, BBox)>();
            var waySegments = new HashSet<(CompleteWay, BBox)>();


            foreach (var (w, bbox) in withBBox)
            {
                waySegments.Clear();
                waySegments.Add((w, bbox));


                foreach (var (otherWay, otherBbox) in withBBox)
                {
                    if (!bbox.OverlapsWith(otherBbox))
                    {
                        continue;
                    }

                    if (w.Id == otherWay.Id)
                    {
                        continue;
                    }

                    newWaySegments.Clear();
                    foreach (var (segment, segmentBbox) in waySegments)
                    {
                        FullSplit(segment, segmentBbox,
                            otherWay, otherBbox,
                            newWaySegments);
                    }

                    var h = waySegments;
                    waySegments = newWaySegments;


                    newWaySegments = h;
                    newWaySegments.Clear();
                }

                foreach (var (segment, _) in waySegments)
                {
                    allSegments.Add(segment);
                }
            }

            return allSegments;
        }
    }
}