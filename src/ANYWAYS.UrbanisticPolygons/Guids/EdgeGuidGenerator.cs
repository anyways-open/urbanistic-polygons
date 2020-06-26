using System;
using System.Collections.Generic;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using ANYWAYS.UrbanisticPolygons.IO;
using ANYWAYS.UrbanisticPolygons.Tiles;

namespace ANYWAYS.UrbanisticPolygons.Guids
{
    internal static class EdgeGuidGenerator
    {
        /// <summary>
        /// The namespace for vertex ids domain names (from RFC 4122, Appendix C).
        /// </summary>
        public static readonly Guid Namespace = new Guid("62b34a03-f1fd-4d04-965f-4e8355a8ac24");
        
        /// <summary>
        /// Builds a guid for the given edge.
        /// </summary>
        /// <param name="enumerator">The enumerator moved to the edge.</param>
        public static Guid GetEdgeGuid(this TiledBarrierGraph.BarrierGraphEnumerator enumerator)
        {
            var bytes = new List<byte>();

            if (!enumerator.Forward)
            {
                var otherEnumerator = enumerator.Graph.GetEnumerator();
                otherEnumerator.MoveTo(enumerator.Vertex2);
                otherEnumerator.MoveNextUntil(enumerator.Edge);
                enumerator = otherEnumerator;
            }
            
            foreach (var c in enumerator.CompleteShape())
            {
                var tiledLocation = TileStatic.ToLocalTileCoordinates(enumerator.Graph.Zoom, c, 16384);
                bytes.AddRange(tiledLocation.GetBytes());
            }
            
            return GuidUtility.Create(Namespace, bytes);
        }
    }
}