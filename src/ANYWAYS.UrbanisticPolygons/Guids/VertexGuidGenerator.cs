using System;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using ANYWAYS.UrbanisticPolygons.Graphs.Polygons;
using ANYWAYS.UrbanisticPolygons.IO;
using ANYWAYS.UrbanisticPolygons.Tiles;

namespace ANYWAYS.UrbanisticPolygons.Guids
{
    internal static class VertexGuidGenerator
    {
        /// <summary>
        /// The namespace for vertex ids domain names (from RFC 4122, Appendix C).
        /// </summary>
        public static readonly Guid Namespace = new Guid("cff2a084-3138-486b-84e4-6f8099cb4c70");
        
        /// <summary>
        /// Builds a guid for the given vertex.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="vertex">The vertex.</param>
        public static Guid GetVertexGuid(this TiledBarrierGraph graph, int vertex)
        {
            // we have a planar graph so location <-> guid.
            // we generate an id based on the vertex location relative in a tile.

            var location = graph.GetVertex(vertex);
            var tileLocation = TileStatic.ToLocalTileCoordinates(graph.Zoom, location, 16384);
            
            return GuidUtility.Create(Namespace, tileLocation.GetBytes());
        }
        
        /// <summary>
        /// Builds a guid for the given vertex.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="vertex">The vertex.</param>
        public static Guid GetVertexGuid(this TiledPolygonGraph graph, int vertex)
        {
            // we have a planar graph so location <-> guid.
            // we generate an id based on the vertex location relative in a tile.

            var tileLocation = graph.GetVertex(vertex);
            
            return GuidUtility.Create(Namespace, tileLocation.GetBytes());
        }
    }
}