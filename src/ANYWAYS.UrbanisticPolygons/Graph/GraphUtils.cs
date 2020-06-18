using System;
using System.Diagnostics.CodeAnalysis;
using OsmSharp;

namespace ANYWAYS.UrbanisticPolygons.Graph
{
    static class GraphUtils
    {
        private const uint _precisionFactor = 1000000;

        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        public static long NodeId(this Node n)
        {
            return
                (long) (n.Latitude.Value * _precisionFactor) +
                (long) (n.Longitude.Value * _precisionFactor * 100 * _precisionFactor);
        }

        public static (long, long) Id(long a, long b)
        {
            return (
                Math.Min(a, b),
                Math.Max(a, b)
            );
        }

        public static (long, long) Id((long a, long b) tpl)
        {
            if (tpl.a < tpl.b)
            {
                return tpl;
            }

            return (tpl.b, tpl.a);
        }
    }
}