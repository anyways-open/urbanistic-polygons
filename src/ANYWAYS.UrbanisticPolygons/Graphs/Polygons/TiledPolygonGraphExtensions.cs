using System;
using System.Collections.Generic;
using System.IO;
using ANYWAYS.UrbanisticPolygons.Guids;
using ANYWAYS.UrbanisticPolygons.IO;
using ANYWAYS.UrbanisticPolygons.Landuse;
using ANYWAYS.UrbanisticPolygons.Tiles;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using OsmSharp.Geo;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons.Graphs.Polygons
{
    internal static class TiledPolygonGraphExtensions
    {
        public static void AddTileFromStream(this TiledPolygonGraph graph, uint tile, Stream stream)
        {
            var buffer = new byte[1024];
            var guid = stream.ReadNullableGuid();
            
            // set tile loaded.
            graph.SetTileLoaded(tile);

            // read vertices until empty guid.
            while (guid != null)
            {
                // a new vertex guid detected.
                var vertexGuid = guid.Value;
                
                // add vertex.
                var vertexTiledLocation = stream.ReadTiledLocation();
                if (!graph.TryGetVertex(vertexGuid, out _))
                {
                    graph.AddVertex(vertexTiledLocation, vertexGuid);
                }
                
                // move to next vertex.
                guid = stream.ReadNullableGuid();
            }
            
            // read edges until empty guid.
            guid = stream.ReadNullableGuid();
            while (guid != null)
            {
                // a new vertex guid detected.
                var vertex1Guid = guid.Value;
                if (!graph.TryGetVertex(vertex1Guid, out var vertex1)) throw new Exception("Vertex1 not in graph!");

                // read edge count.
                var edges = stream.ReadInt32();
                for (var e = 0; e < edges; e++)
                {
                    var edgeGuid = stream.ReadGuid();
                    var vertex2Guid = stream.ReadGuid();
                    if (!graph.TryGetVertex(vertex2Guid, out var vertex2)) throw new Exception("Vertex1 not in graph!");

                    var shapes = stream.ReadInt32();
                    var shape = new (int x, int y, uint tileId)[shapes];
                    for (var s = 0; s < shapes; s++)
                    {
                        shape[s] = stream.ReadTiledLocation();
                    }

                    var tagCount = stream.ReadInt32();
                    var tagsCollection = new TagsCollection();
                    for (var t = 0; t < tagCount; t++)
                    {
                        tagsCollection.Add(
                            stream.ReadWithSizeString(buffer),
                            stream.ReadWithSizeString(buffer));
                    }

                    if (!graph.TryGetEdge(edgeGuid, out _))
                    {
                        graph.AddEdge(vertex1, vertex2, edgeGuid, shape, tagsCollection);
                    }
                }
                
                guid = stream.ReadNullableGuid();
            }

            // add the default face if not there yet.
            if (graph.FaceCount == 0) graph.AddFace(); 
            
            // read faces.
            guid = stream.ReadNullableGuid();
            while (guid != null)
            {
                // check if the face exists.
                int? face = null;
                if (!graph.TryGetFace(guid.Value, out _))
                {
                    face = graph.AddFace(guid.Value);
                }
                
                // read all edges.
                var edges = stream.ReadInt32();
                for (var e = 0; e < edges; e++)
                {
                    var edgeGuid = stream.ReadGuid();
                    var forward = stream.ReadByte() == 1;

                    if (!graph.TryGetEdge(edgeGuid, out var edgeId))
                    {
                        throw new Exception("Edge in face not found!");
                    }

                    // add only if face didn't exist yet.
                    if (face.HasValue) graph.SetFace(edgeId, !forward, face.Value);
                }
                
                // read attributes.
                var attributes = stream.ReadAttributes();
                if (face.HasValue) graph.SetFaceData(face.Value, attributes);
                
                guid = stream.ReadNullableGuid();
            }
        }

        private static bool IsEmptyGuid(this IReadOnlyList<byte> buffer)
        {
            for (var i = 0; i < 16; i++)
            {
                if (buffer[i] != 255) return false;
            }

            return true;
        }
        
        internal static IEnumerable<Feature> ToFeatures(this TiledPolygonGraph graph)
        {
            var enumerator = graph.GetEnumerator();
            for (var v = 0; v < graph.VertexCount; v++)
            {
                if (!enumerator.MoveTo(v)) continue;

                bool hasEdge = false;
                while (enumerator.MoveNext())
                {
                    hasEdge = true;
                    if (!enumerator.Forward) continue;

                    var lineString = enumerator.ToLineString();
                    var attributes = enumerator.ToAttributeTable();

                    yield return new Feature(lineString, attributes);
                }
                
                if (hasEdge) yield return new Feature(graph.ToPoint(v), new AttributesTable {{"vertex", v}, {"vertex_guid", graph.GetVertexGuid(v)}});
            }

            foreach (var polygon in graph.GetAllPolygons())
            {
                yield return polygon.feature;
            }
            
            foreach (var tileFeature in graph.ToTileFeatures())
            {
                yield return tileFeature;
            }
        }
        
        internal static IEnumerable<(Feature feature, Guid id)> GetAllPolygons(this TiledPolygonGraph graph)
        {
            // for every face, determine polygon.
            for (var f = 1; f < graph.FaceCount; f++)
            {
                var polygon = graph.ToPolygon(f);
                if (polygon == null) continue;
                
                yield return polygon.Value;
            }
        }

        public static (Feature feature, Guid id)? ToPolygon(this TiledPolygonGraph graph, int face)
        {
            var coordinates = new List<Coordinate>();
            foreach (var c in graph.FaceToClockwiseCoordinates(face))
            {
                var cLocation = c.FromLocalTileCoordinates(graph.Zoom, graph.Resolution);
                coordinates.Add(new Coordinate(cLocation.longitude, cLocation.latitude));
            }

            if (coordinates.Count <= 3) return null;

            var faceGuid = graph.GetFaceGuid(face);
            if (faceGuid == null) return null;
            
            var attributes = new AttributesTable {{"face", face}, {"face_guid", faceGuid}};
            var faceAttributes = graph.GetFaceData(face);
            foreach (var (type, per) in faceAttributes)
            {
                attributes.Add(type, per);
            }
            
            return (new Feature(new NetTopologySuite.Geometries.Polygon(new LinearRing(coordinates.ToArray())), 
                attributes), faceGuid.Value);
        }

        public static IEnumerable<(int x, int y, uint tileId)> FaceToClockwiseCoordinates(
            this TiledPolygonGraph graph, int face)
        {
            var edges = graph.EnumerateFaceClockwise(face);

            var firstReturned = false;
            foreach (var edge in edges)
            {
                if (!firstReturned)
                {
                    yield return graph.GetVertex(edge.vertex1);
                    firstReturned = true;
                }

                for (var s = 0; s < edge.shape.Length; s++)
                {
                    var i = s;
                    if (!edge.forward) i = edge.shape.Length - i - 1;
                    var sp = edge.shape[i];
                    yield return sp;
                }
                
                yield return graph.GetVertex(edge.vertex2);
            }
        }

        public static
            IEnumerable<(int vertex1, int edge, bool forward, int vertex2, (int x, int y, uint tileId)[] shape)>
            EnumerateFaceClockwise(
                this TiledPolygonGraph graph, int face)
        {
            var enumerator = graph.GetFaceEnumerator();
            if (!enumerator.MoveTo(face)) yield break;
            if (face == 0) yield break;
            
            var edges = new List<(int vertex1, int edge, bool forward, int vertex2, (int x, int y, uint tileId)[] shape)>();
            while (enumerator.MoveNext())
            {
                if (enumerator.IsLeft)
                {
                    edges.Add((enumerator.Vertex2, enumerator.Edge, false, enumerator.Vertex1, enumerator.Shape));
                }
                else
                {
                    edges.Add((enumerator.Vertex1, enumerator.Edge, true, enumerator.Vertex2, enumerator.Shape));
                }
            }

            if (edges.Count <= 1) yield break;
            if (edges[0].vertex1 == edges[1].vertex2) edges.Reverse();
            if (edges[0].vertex1 != edges[^1].vertex2) yield break;

            foreach (var edge in edges)
            {
                yield return edge;
            }
        }

        private static IEnumerable<Feature> ToTileFeatures(this TiledPolygonGraph graph)
        {
            foreach (var tile in graph.LoadedTiles())
            {
                var box = TileStatic.Box(graph.Zoom, tile);
                var polygon = new NetTopologySuite.Geometries.Polygon(new LinearRing(new []
                {
                    new Coordinate(box.topLeft.longitude, box.topLeft.latitude), 
                    new Coordinate(box.bottomRight.longitude, box.topLeft.latitude), 
                    new Coordinate(box.bottomRight.longitude, box.bottomRight.latitude), 
                    new Coordinate(box.topLeft.longitude, box.bottomRight.latitude), 
                    new Coordinate(box.topLeft.longitude, box.topLeft.latitude)
                }));
            
                yield return new Feature(polygon, new AttributesTable{{"tile_id", tile},{"zoom", graph.Zoom}});
            }
        }

        internal static Point ToPoint(this TiledPolygonGraph graph, int vertex)
        {
            var location = graph.GetVertex(vertex).FromLocalTileCoordinates(graph.Zoom, graph.Resolution);
            return new Point(new Coordinate(location.longitude, location.latitude));
        }
        
        internal static LineString ToLineString(this TiledPolygonGraph.GraphEnumerator enumerator)
        {
            var graph = enumerator.Graph;
            var coordinates = new Coordinate[enumerator.Shape.Length + 2];

            var vertex1Location = enumerator.Graph.GetVertex(enumerator.Vertex1).FromLocalTileCoordinates(graph.Zoom, graph.Resolution);
            coordinates[0] = new Coordinate(vertex1Location.longitude, vertex1Location.latitude);

            for (var s = 0; s < enumerator.Shape.Length; s++)
            {
                var i = s;
                if (!enumerator.Forward)
                {
                    i = enumerator.Shape.Length - s;
                }

                var sp = enumerator.Shape[i].FromLocalTileCoordinates(graph.Zoom, graph.Resolution);
                coordinates[i + 1] = new Coordinate(sp.longitude, sp.latitude);
            }

            var vertex2Location = enumerator.Graph.GetVertex(enumerator.Vertex2).FromLocalTileCoordinates(graph.Zoom, graph.Resolution);
            coordinates[^1] = new Coordinate(vertex2Location.longitude, vertex2Location.latitude);
            
            return new LineString(coordinates);
        }

        internal static AttributesTable ToAttributeTable(this TiledPolygonGraph.GraphEnumerator enumerator)
        {
            var attributes = enumerator.Tags.ToAttributeTable();

            attributes.Add("face_left", enumerator.FaceLeft);
            attributes.Add("face_right", enumerator.FaceRight);
            
            return attributes;
        }
    }
}