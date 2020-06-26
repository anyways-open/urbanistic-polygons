// using System;
// using OsmSharp.Complete;
//
// namespace ANYWAYS.UrbanisticPolygons
// {
//     public class BBox
//     {
//         public readonly double MinLat, MaxLat, MinLon, MaxLon;
//
//         public BBox(double minLat, double maxLat, double minLon, double maxLon)
//         {
//             MinLat = minLat;
//             MaxLat = maxLat;
//             MinLon = minLon;
//             MaxLon = maxLon;
//         }
//
//         public BBox(CompleteWay w)
//         {
//             MinLat = double.MaxValue;
//             MinLon = double.MaxValue;
//             MaxLat = double.MinValue;
//             MaxLon = double.MinValue;
//
//             
//             foreach (var node in w.Nodes)
//             {
//                 MinLat = Math.Min(MinLat, node.Latitude.Value);
//                 MinLon = Math.Min(MinLon, node.Longitude.Value);
//                 MaxLat = Math.Max(MaxLat, node.Latitude.Value);
//                 MaxLon = Math.Max(MaxLon, node.Longitude.Value);
//             }
//         }
//
//         public bool OverlapsWith(BBox other)
//         {
//             if (other.MaxLat < other.MinLat ||
//                 other.MaxLon < other.MinLon ||
//                 MaxLat < other.MinLat ||
//                 MaxLon < other.MinLon)
//             {
//                 return false;
//             }
//             return true;
//         }
//     }
// }