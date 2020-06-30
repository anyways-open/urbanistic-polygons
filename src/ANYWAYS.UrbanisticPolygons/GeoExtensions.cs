using System;
using System.Collections.Generic;

namespace ANYWAYS.UrbanisticPolygons
{
    /// <summary>
    /// Contains extension methods to work with coordinates, lines, bounding boxes and basic spatial operations.
    /// </summary>
    internal static class GeoExtensions
    {
        private const double E = 0.00000000000001;
        private const double RadiusOfEarth = 6371000;

        public static bool IsLeftOf(this (double longitude, double latitude) coordinate1,
            (double longitude, double latitude) coordinate2)
        {
            return coordinate1.longitude < coordinate2.longitude;
        }

        public static double Angle((double longitude, double latitude) coordinate1,
            (double longitude, double latitude) coordinate2,
            (double longitude, double latitude) coordinate3)
        {
            var v11 = coordinate1.latitude - coordinate2.latitude;
            var v10 = coordinate1.longitude - coordinate2.longitude;

            var minDiff = 0.000001; 
            if (Math.Abs(v11) < minDiff || Math.Abs(v10) < minDiff)
            {
                var factor = System.Math.Max(
                    (minDiff * 2) / Math.Abs(v11), 
                    (minDiff * 2) / Math.Abs(v10));
                v11 *= factor;
                v10 *= factor;
            }

            var v21 = coordinate3.latitude - coordinate2.latitude;
            var v20 = coordinate3.longitude - coordinate2.longitude;
            
            if (Math.Abs(v21) < minDiff || Math.Abs(v20) < minDiff)
            {
                var factor = System.Math.Max(
                    (minDiff * 2) / Math.Abs(v21), 
                    (minDiff * 2) / Math.Abs(v20));
                v21 *= factor;
                v20 *= factor;
            }

            var v1size = System.Math.Sqrt(v11 * v11 + v10 * v10);
            var v2size = System.Math.Sqrt(v21 * v21 + v20 * v20);

            if (Math.Abs(v1size) < E || Math.Abs(v2size) < E)
            {
                return double.NaN;
            }

            var dot = (v11 * v21 + v10 * v20);
            var cross = (v10 * v21 - v11 * v20);

            if (Math.Abs(cross) < E)
            {
                // The cross product is pretty small, the points are close to each other
                // This either means we are at 180° or 360°, depending on the dot product
                if (dot < 0)
                {
                    return Math.PI;
                }
                else
                {
                    return (2 * Math.PI);
                }
            }

            if (Math.Abs(dot) < E)
            {
                // The dot-product is pretty small or close to zero -> the coordinates are perpendicular
                // only thing left to figure out if the angle is positive or negative
                // For this we have the cross-product
                if (cross > 0)
                {
                    return (Math.PI / 2);
                }
                else
                {
                    return (3 * Math.PI / 2);
                }
            }

            // split per quadrant.
            double angle;
            if (dot > 0)
            {
                // dot > 0
                if (cross > 0)
                {
                    // dot > 0 and cross > 0
                    // Quadrant 1
                    angle = System.Math.Asin(cross / (v1size * v2size));
                    if (angle < System.Math.PI / 4f)
                    {
                        // use cosine.
                        angle = System.Math.Acos(dot / (v1size * v2size));
                    }

                    // angle is ok here for quadrant 1.
                }
                else
                {
                    // dot > 0 and cross <= 0
                    // Quadrant 4
                    angle = (System.Math.PI * 2.0f) + System.Math.Asin(cross / (v1size * v2size));
                    if (angle > (System.Math.PI * 2.0f) - System.Math.PI / 4f)
                    {
                        // use cosine.
                        angle = (System.Math.PI * 2.0f) - System.Math.Acos(dot / (v1size * v2size));
                    }

                    // angle is ok here for quadrant 1.
                }
            }
            else
            {
                // dot <= 0
                if (cross > 0)
                {
                    // dot > 0 and cross > 0
                    // Quadrant 2
                    angle = System.Math.PI - System.Math.Asin(cross / (v1size * v2size));
                    if (angle > System.Math.PI / 2f + System.Math.PI / 4f)
                    {
                        // use cosine.
                        angle = System.Math.Acos(dot / (v1size * v2size));
                    }

                    // angle is ok here for quadrant 2.
                }
                else
                {
                    // dot > 0 and cross <= 0
                    // Quadrant 3
                    angle = -(-System.Math.PI + System.Math.Asin(cross / (v1size * v2size)));
                    if (angle < System.Math.PI + System.Math.PI / 4f)
                    {
                        // use cosine.
                        angle = (System.Math.PI * 2.0f) - System.Math.Acos(dot / (v1size * v2size));
                    }

                    // angle is ok here for quadrant 3.
                }
            }

            return angle;
        }

        /// <summary>
        /// Returns an estimate of the distance between the two given coordinates.
        /// </summary>
        /// <param name="coordinate1">The first coordinate.</param>
        /// <param name="coordinate2">The second coordinate.</param>
        /// <remarks>Accuracy decreases with distance.</remarks>
        public static double DistanceEstimateInMeter(this (double longitude, double latitude) coordinate1,
            (double longitude, double latitude) coordinate2)
        {
            var lat1Rad = (coordinate1.latitude / 180d) * System.Math.PI;
            var lon1Rad = (coordinate1.longitude / 180d) * System.Math.PI;
            var lat2Rad = (coordinate2.latitude / 180d) * System.Math.PI;
            var lon2Rad = (coordinate2.longitude / 180d) * System.Math.PI;

            var x = (lon2Rad - lon1Rad) * System.Math.Cos((lat1Rad + lat2Rad) / 2.0);
            var y = lat2Rad - lat1Rad;

            var m = System.Math.Sqrt(x * x + y * y) * RadiusOfEarth;

            return m;
        }

        /// <summary>
        /// Returns a coordinate offset with a given distance.
        /// </summary>
        /// <param name="coordinate">The coordinate.</param>
        /// <param name="meter">The distance.</param>
        /// <returns>An offset coordinate.</returns>
        public static (double longitude, double latitude) OffsetWithDistanceX(
            this (double longitude, double latitude) coordinate, double meter)
        {
            var offset = 0.001;
            var offsetLon = (coordinate.longitude + offset, coordinate.latitude);
            var lonDistance = offsetLon.DistanceEstimateInMeter(coordinate);

            return (coordinate.longitude + (meter / lonDistance) * offset,
                coordinate.latitude);
        }

//         
//         internal static double DistanceEstimateInMeterShape(this (double longitude, double latitude) coordinate1, 
//             (double longitude, double latitude) coordinate2, IEnumerable<(double longitude, double latitude)>? shape = null)
//         {
//             if (shape == null) return coordinate1.DistanceEstimateInMeter(coordinate2);
//             
//             var distance = 0.0;
//             
//             using var shapeEnumerator = shape.GetEnumerator();
//             var previous = coordinate1;
//
//             while (shapeEnumerator.MoveNext())
//             {
//                 var current = shapeEnumerator.Current;
//                 distance += previous.DistanceEstimateInMeter(current);
//                 previous = current;
//             }
//
//             distance += previous.DistanceEstimateInMeter(coordinate2);
//
//             return distance;
//         }
//         
//         /// <summary>
//         /// Returns an estimate of the length of the given linestring.
//         /// </summary>
//         /// <param name="lineString">The linestring.</param>
//         /// <remarks>Accuracy decreases with distance.</remarks>
//         public static double DistanceEstimateInMeter(this IEnumerable<(double longitude, double latitude)> lineString)
//         {
//             var distance = 0.0;
//             
//             using var shapeEnumerator = lineString.GetEnumerator();
//             shapeEnumerator.MoveNext();
//             var previous = shapeEnumerator.Current;
//
//             while (shapeEnumerator.MoveNext())
//             {
//                 var current = shapeEnumerator.Current;
//                 distance += previous.DistanceEstimateInMeter(current);
//                 previous = current;
//             }
//
//             return distance;
//         }
//         

//         
//         /// <summary>
//         /// Returns a coordinate offset with a given distance.
//         /// </summary>
//         /// <param name="coordinate">The coordinate.</param>
//         /// <param name="meter">The distance.</param>
//         /// <returns>An offset coordinate.</returns>
//         public static (double longitude, double latitude) OffsetWithDistanceY(this (double longitude, double latitude) coordinate, double meter)
//         {
//             var offset = 0.001;
//             var offsetLat = (coordinate.longitude, coordinate.latitude + offset);
//             var latDistance = offsetLat.DistanceEstimateInMeter(coordinate);
//
//             return (coordinate.longitude, 
//                 coordinate.latitude + (meter / latDistance) * offset);
//         }
//         
//         /// <summary>
//         /// Calculates an offset position along the line segment.
//         /// </summary>
//         /// <param name="line">The line segment.</param>
//         /// <param name="offset">The offset [0,1].</param>
//         /// <returns>The offset coordinate.</returns>
//         public static (double longitude, double latitude) PositionAlongLine(this ((double longitude, double latitude) coordinate1, 
//             (double longitude, double latitude) coordinate2) line, double offset)
//         {
//             var coordinate1 = line.coordinate1;
//             var coordinate2 = line.coordinate2;
//             
//             var latitude = coordinate1.latitude + ((coordinate2.latitude - coordinate1.latitude) * offset);
//             var longitude = coordinate1.longitude + ((coordinate2.longitude - coordinate1.longitude) * offset);
// //            short? elevation = null;
// //            if (coordinate1.Elevation.HasValue &&
// //                coordinate2.Elevation.HasValue)
// //            {
// //                elevation = (short)(coordinate1.Elevation.Value - ((coordinate2.Elevation.Value - coordinate1.Elevation.Value) * offset));
// //            }
//
//             return (longitude, latitude); //, elevation);
//         }
//         
//         
//         /// <summary>
//         /// Projects for coordinate on this line.
//         /// </summary>
//         /// <param name="line">The line.</param>
//         /// <param name="coordinate">The coordinate.</param>
//         /// <returns>The project coordinate.</returns>
//         public static (double longitude, double latitude)? ProjectOn(this ((double longitude, double latitude) coordinate1, 
//             (double longitude, double latitude) coordinate2) line, (double longitude, double latitude) coordinate)
//         {
//             var coordinate1 = line.coordinate1;
//             var coordinate2 = line.coordinate2;
//             
//             // TODO: do we need to calculate the expensive length in meter, this can be done more easily.
//             var lengthInMeters = line.coordinate1.DistanceEstimateInMeter(line.coordinate2);
//             if (lengthInMeters < E)
//             { 
//                 return null;
//             }
//
//             // get direction vector.
//             var diffLat = (coordinate2.latitude - coordinate1.latitude);
//             var diffLon = (coordinate2.longitude - coordinate1.longitude);
//
//             // increase this line in length if needed.
//             var longerLine = line;
//             if (lengthInMeters < 50)
//             {
//                 longerLine = (coordinate1, (diffLon + coordinate.longitude, diffLat + coordinate.latitude));
//             }
//
//             // rotate 90°, offset y with x, and x with y.
//             var xLength = longerLine.coordinate1.DistanceEstimateInMeter((longerLine.coordinate2.longitude, longerLine.coordinate1.latitude));
//             if (longerLine.coordinate1.longitude > longerLine.coordinate2.longitude) xLength = -xLength;
//             var yLength = longerLine.coordinate1.DistanceEstimateInMeter((longerLine.coordinate1.longitude, longerLine.coordinate2.latitude));
//             if (longerLine.coordinate1.latitude > longerLine.coordinate2.latitude) yLength = -yLength;
//             
//             var second = coordinate.OffsetWithDistanceY(xLength)
//                 .OffsetWithDistanceX(-yLength);
//
//             // create a second line.
//             var other = (coordinate, second);
//
//             // calculate intersection.
//             var projected = longerLine.Intersect(other, false);
//
//             // check if coordinate is on this line.
//             if (!projected.HasValue)
//             {
//                 return null;
//             }
//             
//             // check if the coordinate is on this line.
//             var dist = line.A() * line.A() + line.B() * line.B();
//             var line1 = (projected.Value, coordinate1);
//             var distTo1 = line1.A() * line1.A() + line1.B() * line1.B();
//             if (distTo1 > dist)
//             {
//                 return null;
//             }
//             var line2 = (projected.Value, coordinate2);
//             var distTo2 = line2.A() * line2.A() + line2.B() * line2.B();
//             if (distTo2 > dist)
//             {
//                 return null;
//             }
//             return projected;
//         }
//
//         /// <summary>
//         /// Returns the center of the box.
//         /// </summary>
//         /// <param name="box">The box.</param>
//         /// <returns>The center.</returns>
//         public static (double longitude, double latitude) Center(
//             this ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box)
//         {
//             return ((box.topLeft.longitude + box.bottomRight.longitude) / 2,
//                 (box.topLeft.latitude + box.bottomRight.latitude) / 2);
//         }
//
//         /// <summary>
//         /// Expands the given box with the other box to encompass both.
//         /// </summary>
//         /// <param name="box">The original box.</param>
//         /// <param name="other">The other box.</param>
//         /// <returns>The expand box or the original box if the other was already contained.</returns>
//         public static ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)
//             Expand(
//                 this ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box,
//                 ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) other)
//         {
//             if (!box.Overlaps(other.topLeft))
//             {
//                 var center = box.Center();
//                 
//                 // handle left.
//                 var left = box.topLeft.longitude;
//                 if (!box.Overlaps((other.topLeft.longitude, center.latitude)))
//                 {
//                     left = other.topLeft.longitude;
//                 }
//                 
//                 // handle top.
//                 var top = box.topLeft.longitude;
//                 if (!box.Overlaps((center.longitude, other.topLeft.latitude)))
//                 {
//                     top = other.topLeft.latitude;
//                 }
//                 
//                 box = ((left, top), box.bottomRight);
//             }
//
//             if (!box.Overlaps(other.bottomRight))
//             {
//                 var center = box.Center();
//                 
//                 // handle right.
//                 var right = box.bottomRight.longitude;
//                 if (!box.Overlaps((other.bottomRight.longitude, center.latitude)))
//                 {
//                     right = other.bottomRight.longitude;
//                 }
//                 
//                 // handle bottom.
//                 var bottom = box.bottomRight.latitude;
//                 if (!box.Overlaps((center.longitude, other.bottomRight.latitude)))
//                 {
//                     bottom = other.bottomRight.latitude;
//                 }
//                 
//                 box = (box.topLeft, (right, bottom));
//             }
//             
//             return box;
//         }
//         
//         /// <summary>
//         /// Calculates the intersection point of the given line with this line. 
//         /// 
//         /// Returns null if the lines have the same direction or don't intersect.
//         /// 
//         /// Assumes the given line is not a segment and this line is a segment.
//         /// </summary>
//         public static (double longitude, double latitude)? Intersect(this ((double longitude, double latitude) coordinate1, 
//             (double longitude, double latitude) coordinate2) thisLine, ((double longitude, double latitude) coordinate1, 
//             (double longitude, double latitude) coordinate2) line, bool checkSegment = true)
//         {
//             var det = (double)(line.A() * thisLine.B() - thisLine.A() * line.B());
//             if (System.Math.Abs(det) <= E)
//             { // lines are parallel; no intersections.
//                 return null;
//             }
//             else
//             { // lines are not the same and not parallel so they will intersect.
//                 var x = (thisLine.B() * line.C() - line.B() * thisLine.C()) / det;
//                 var y = (line.A() * thisLine.C() - thisLine.A() * line.C()) / det;
//
//                 var coordinate = (x ,y);
//
//                 // check if the coordinate is on this line.
//                 if (checkSegment)
//                 {
//                     var dist = thisLine.A() * thisLine.A() + thisLine.B() * thisLine.B();
//                     var line1 = (coordinate, thisLine.coordinate1);
//                     var distTo1 = line1.A() * line1.A() + line1.B() * line1.B();
//                     if (distTo1 > dist)
//                     {
//                         return null;
//                     }
//
//                     var line2 = (coordinate, thisLine.coordinate2);
//                     var distTo2 = line2.A() * line2.A() + line2.B() * line2.B();
//                     if (distTo2 > dist)
//                     {
//                         return null;
//                     }
//                 }
//
// //                if (!_coordinate1.Elevation.HasValue || !_coordinate2.Elevation.HasValue) return coordinate;
// //                
// //                if (_coordinate1.Elevation == _coordinate2.Elevation)
// //                { // don't calculate anything, elevation is identical.
// //                    coordinate.Elevation = _coordinate1.Elevation;
// //                }
// //                else if (System.Math.Abs(this.A) < E && System.Math.Abs(this.B) < E)
// //                { // tiny segment, not stable to calculate offset
// //                    coordinate.Elevation = _coordinate1.Elevation;
// //                }
// //                else
// //                { // calculate offset and calculate an estimate of the elevation.
// //                    if (System.Math.Abs(this.A) > System.Math.Abs(this.B))
// //                    {
// //                        var diffLat = System.Math.Abs(this.A);
// //                        var diffLatIntersection = System.Math.Abs(coordinate.Latitude - _coordinate1.Latitude);
// //
// //                        coordinate.Elevation = (short)((_coordinate2.Elevation - _coordinate1.Elevation) * (diffLatIntersection / diffLat) +
// //                                                       _coordinate1.Elevation);
// //                    }
// //                    else
// //                    {
// //                        var diffLon = System.Math.Abs(this.B);
// //                        var diffLonIntersection = System.Math.Abs(coordinate.Longitude - _coordinate1.Longitude);
// //
// //                        coordinate.Elevation = (short)((_coordinate2.Elevation - _coordinate1.Elevation) * (diffLonIntersection / diffLon) +
// //                                                       _coordinate1.Elevation);
// //                    }
// //                }
//                 return coordinate;
//             }
//         }
//
//         private static double A(this ((double longitude, double latitude) coordinate1,
//             (double longitude, double latitude) coordinate2) line)
//         {
//             return line.coordinate2.latitude - line.coordinate1.latitude;
//         }
//
//         private static double B(this ((double longitude, double latitude) coordinate1,
//             (double longitude, double latitude) coordinate2) line)
//         {
//             return line.coordinate1.longitude - line.coordinate2.longitude;
//         }
//
//         private static double C(this ((double longitude, double latitude) coordinate1,
//             (double longitude, double latitude) coordinate2) line)
//         {
//             return line.A() * line.coordinate1.longitude +
//                    line.B() * line.coordinate1.latitude;
//         }
//         
//         /// <summary>
//         /// Creates a box around this coordinate with width/height approximately the given size in meter.
//         /// </summary>
//         /// <param name="coordinate">The coordinate.</param>
//         /// <param name="sizeInMeters">The size in meter.</param>
//         /// <returns>The size in meter.</returns>
//         public static ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) 
//             BoxAround(this (double longitude, double latitude) coordinate, double sizeInMeters)
//         {
//             var offsetLat = (coordinate.longitude, coordinate.latitude + 0.1);
//             var offsetLon = (coordinate.longitude + 0.1, coordinate.latitude);
//             var latDistance = offsetLat.DistanceEstimateInMeter(coordinate);
//             var lonDistance = offsetLon.DistanceEstimateInMeter(coordinate);
//             
//             return ((coordinate.longitude - (sizeInMeters / lonDistance) * 0.1, 
//                 coordinate.latitude + (sizeInMeters / latDistance) * 0.1),
//                 (coordinate.longitude + (sizeInMeters / lonDistance) * 0.1, 
//                 coordinate.latitude - (sizeInMeters / latDistance) * 0.1));
//         }
//

        public static ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)
            Expand(this ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box,
                ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) other)
        {
            var left = box.topLeft.longitude;
            var top = box.topLeft.latitude;
            var right = box.bottomRight.longitude;
            var bottom = box.bottomRight.latitude;

            if (left > other.topLeft.longitude) left = other.topLeft.longitude;
            if (right < other.bottomRight.longitude) right = other.bottomRight.longitude;
            if (top < other.topLeft.latitude) top = other.topLeft.latitude;
            if (bottom > other.bottomRight.latitude) bottom = other.bottomRight.latitude;

            return ((left, top), (right, bottom));
        }

        public static ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)?
            ToBox(this IEnumerable<(double longitude, double latitude)> coordinates)
        {
            using var enumerator = coordinates.GetEnumerator();
            if (!enumerator.MoveNext())
                return null;
            var top = enumerator.Current.latitude;
            var left = enumerator.Current.longitude;
            var bottom = top;
            var right = left;

            while (enumerator.MoveNext())
            {
                var (lon, lat) = enumerator.Current;
                if (top < lat) top = lat;
                if (bottom > lat) bottom = lat;
                if (left > lon) left = lon;
                if (right < lon) right = lon;
            }

            return ((left, top), (right, bottom));
        }
        
        public static bool Overlaps(
            this ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box,
            (double longitude, double latitude) coordinate)
        {
            return box.bottomRight.latitude < coordinate.latitude && coordinate.latitude <= box.topLeft.latitude &&
                   box.topLeft.longitude < coordinate.longitude && coordinate.longitude <= box.bottomRight.longitude;
        }
        
        public static bool Overlaps(
            this ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box,
            ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) other)
        {
            var e = 0.00000001;
            var diff = box.topLeft.longitude - other.bottomRight.longitude;
            if (diff > e) return false;
            diff = other.bottomRight.latitude - box.topLeft.latitude;
            //if (box.topLeft.latitude <= other.bottomRight.latitude) return false;
            if (diff > e) return false;
            diff = other.topLeft.longitude - box.bottomRight.longitude;
            //if (box.bottomRight.longitude <= other.topLeft.longitude) return false;
            if (diff > e) return false;
            //if (box.bottomRight.latitude >= other.topLeft.latitude) return false;
            diff = box.bottomRight.latitude - other.topLeft.latitude;
            if (diff > e) return false;
            
            return true;
        }
    }
}