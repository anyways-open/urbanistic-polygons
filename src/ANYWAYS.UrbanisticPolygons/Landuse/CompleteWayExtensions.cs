using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using OsmSharp.Complete;

namespace ANYWAYS.UrbanisticPolygons.Landuse
{
    internal static class CompleteWayExtensions
    {
        internal static Polygon? ToPolygon(this CompleteWay way)
        {
            if (way.Nodes[0] != way.Nodes[^1]) return null;

            var coordinates = new List<Coordinate>();
            foreach (var node in way.Nodes)
            {
                if (node.Latitude == null || node.Longitude == null) throw new Exception("Node doesn't have a valid location.");
                
                coordinates.Add(new Coordinate(node.Longitude.Value, node.Latitude.Value));
            }
            
            return new Polygon(new LinearRing(coordinates.ToArray()));
        }
        
        private static bool IsClockwise(this CompleteWay way)
        {
            return way.SignedArea() < 0;
        }
        
        private static double SignedArea(this CompleteWay way)
        {
            if (way.Nodes[0] != way.Nodes[^1]) throw new ArgumentException("First and last node don't match");

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
    }
}