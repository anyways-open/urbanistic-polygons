using System;
using System.IO;
using System.Linq;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Faces;
using ANYWAYS.UrbanisticPolygons.Guids;
using ANYWAYS.UrbanisticPolygons.IO;
using ANYWAYS.UrbanisticPolygons.Landuse;
using ANYWAYS.UrbanisticPolygons.Tiles;

namespace ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Serialization
{
    internal static class TiledBarrierGraphSerializer
    {
        private static readonly byte[] EmptyGuid = {255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255};
        
        public static void WriteTileTo(this TiledBarrierGraph graph, Stream stream, uint tile)
        {
            var enumerator = graph.GetEnumerator();
            
            // write vertices.
            for (var v = 0; v < graph.VertexCount; v++)
            {
                if (!enumerator.MoveTo(v)) continue;
                if (!enumerator.MoveNext()) continue;
                
                // write vertex details.
                var vertexGuid = graph.GetVertexGuid(v);
                stream.Write(vertexGuid.ToByteArray());
                stream.Write(graph.GetVertexLocationBytes(v));
            }
            stream.Write(EmptyGuid);
            
            // write edges.
            for (var v = 0; v < graph.VertexCount; v++)
            {
                if (!enumerator.MoveTo(v)) continue;
                
                // check (and count) edges.
                var edges = 0;
                while (enumerator.MoveNext())
                {
                    if (enumerator.Forward) edges++;
                }
                if (edges == 0) continue;
                
                // write vertex1 details.
                var vertex1Guid = graph.GetVertexGuid(v);
                stream.Write(vertex1Guid.ToByteArray());
                
                // write edges details.
                stream.Write(BitConverter.GetBytes(edges)); // the number of edges.
                enumerator.MoveTo(v);
                while (enumerator.MoveNext())
                {
                    if (!enumerator.Forward) continue;
                    
                    enumerator.WriteToStream(stream);
                }
            }
            stream.Write(EmptyGuid);
            
            // write faces.
            for (var f = 1; f < graph.FaceCount; f++)
            {
                var faceGuid = graph.GetFaceGuid(f);
                if (faceGuid == null) continue;
                stream.Write(faceGuid.Value.ToByteArray());
                
                // write edges.
                var edges = graph.EnumerateFaceClockwise(f).ToList();
                stream.Write(BitConverter.GetBytes(edges.Count));
                foreach (var edge in edges)
                {
                    enumerator.MoveTo(edge.vertex1);
                    enumerator.MoveNextUntil(edge.edge);
                    
                    stream.Write(enumerator.GetEdgeGuid().ToByteArray());
                    stream.WriteByte(edge.forward ? (byte)1 :  (byte)0);
                }
                
                // write attributes.
                stream.WriteAttributes(graph.GetFaceData(f));
            }
            stream.Write(EmptyGuid);
        }

        private static byte[] GetVertexLocationBytes(this TiledBarrierGraph graph, int v)
        {
            return TileStatic.ToLocalTileCoordinates(graph.Zoom, graph.GetVertex(v), 16384).GetBytes();
        }

        public static void WriteToStream(this TiledBarrierGraph.BarrierGraphEnumerator enumerator, Stream stream)
        {
            var graph = enumerator.Graph;
            
            stream.Write(enumerator.GetEdgeGuid().ToByteArray());

            var vertex2Guid = graph.GetVertexGuid(enumerator.Vertex2);
            stream.Write(vertex2Guid.ToByteArray());

            var shape = enumerator.Shape;
            stream.Write(BitConverter.GetBytes(shape.Length));
            for (var s = 0; s < shape.Length; s++)
            {
                stream.Write(TileStatic.ToLocalTileCoordinates(graph.Zoom, shape[s], 16384).GetBytes());
            }

            var tags = enumerator.Tags;
            stream.Write(BitConverter.GetBytes(tags.Count));
            foreach (var tag in tags)
            {
                stream.WriteWithSize(tag.Key);
                stream.WriteWithSize(tag.Value);
            }
        }
    }
}