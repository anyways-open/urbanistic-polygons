using System;
using System.Collections.Generic;
using System.Linq;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Faces;
using ANYWAYS.UrbanisticPolygons.Graphs.Polygons;
using ANYWAYS.UrbanisticPolygons.IO;
using ANYWAYS.UrbanisticPolygons.Tiles;
using GeoAPI.Geometries;
using Coordinate = NetTopologySuite.Geometries.Coordinate;

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
        public static Guid? GetFaceGuid(this TiledBarrierGraph graph, int face)
        {
            var locations = graph.FaceToClockwiseCoordinates(face).Select(x => 
                TileStatic.ToLocalTileCoordinates(14, x, 16384)).ToArray();

            return GetFaceGuidFor(locations);
        }
        
        /// <summary>
        /// Builds a guid for the given face.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="face">The face.</param>
        public static Guid? GetFaceGuid(this TiledPolygonGraph graph, int face)
        {
            return GetFaceGuidFor(graph.FaceToClockwiseCoordinates(face).ToArray());
        }

        private static Guid? GetFaceGuidFor((int x, int y, uint tileId)[] locations)
        {
            if (locations.Length == 0) return null;
            
            // find the most top-left coordinate and use that as the start.
            ((int x, int y, uint tileId) location, int i) topLeft = (locations[0], 0);
            for (var i = 1; i < locations.Length; i++)
            {
                var l = locations[i];
                var c = topLeft.location.CompareTopLeft(l);
                if (c <= 0) continue;

                topLeft = (l, i);
            }
            
            var bytes = new List<byte>();
            
            // enumerate from found index.
            for (var i = topLeft.i; i < locations.Length - 1; i++)
            {
                var tiledLocation = locations[i];
                
                bytes.AddRange(tiledLocation.GetBytes());
            }
            // enumerate to found index.
            for (var i = 0; i < topLeft.i; i++)
            {
                var tiledLocation = locations[i];
                
                bytes.AddRange(tiledLocation.GetBytes());
            }
            
            return GuidUtility.Create(Namespace, bytes);
        }
    }
}