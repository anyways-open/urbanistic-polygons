using System;
using System.Collections.Generic;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Faces;
using ANYWAYS.UrbanisticPolygons.Graphs.Polygons;
using ANYWAYS.UrbanisticPolygons.IO;
using ANYWAYS.UrbanisticPolygons.Tiles;

namespace ANYWAYS.UrbanisticPolygons.Guids
{
    internal static class FaceGuidGenerator
    {
        /// <summary>
        /// The namespace for vertex ids domain names (from RFC 4122, Appendix C).
        /// </summary>
        public static readonly Guid Namespace = new Guid("2115f6f1-20c3-46e0-9f82-863ba536dee9");
        
        /// <summary>
        /// Builds a guid for the given face.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="face">The face.</param>
        public static Guid GetFaceGuid(this TiledBarrierGraph graph, int face)
        {
            var bytes = new List<byte>();
            foreach (var c in graph.FaceToClockwiseCoordinates(face))
            {
                var tiledLocation = TileStatic.ToLocalTileCoordinates(graph.Zoom, c, 16384);
                
                bytes.AddRange(tiledLocation.GetBytes());
            }
            
            return GuidUtility.Create(Namespace, bytes);
        }
        
        /// <summary>
        /// Builds a guid for the given face.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="face">The face.</param>
        public static Guid GetFaceGuid(this TiledPolygonGraph graph, int face)
        {
            var bytes = new List<byte>();
            foreach (var c in graph.FaceToClockwiseCoordinates(face))
            {
                bytes.AddRange(c.GetBytes());
            }
            
            return GuidUtility.Create(Namespace, bytes);
        }
    }
}