using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ANYWAYS.UrbanisticPolygons.Tiles;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

[assembly:InternalsVisibleTo("ANYWAYS.UrbanisticPolygons.Tests.Functional")]
namespace ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Faces
{
    internal static class Faces
    {
        public static (bool success, IEnumerable<uint> missingTiles) AssignFaces(this TiledBarrierGraph graph, uint tile)
        {
            if (!graph.HasTile(tile)) return (false, new[] {tile});

            var facesUnassigned = true;
            while (facesUnassigned)
            {
                facesUnassigned = false;
                graph.ResetFaces();

                // the default face for the case where a loop cannot be found.
                var unAssignableFace = graph.AddFace();
                
                var enumerator = graph.GetEnumerator();
                for (var v = 0; v < graph.VertexCount; v++)
                {
                    if (facesUnassigned) break;
                    
                    if (!enumerator.MoveTo(v)) continue;
                    if (!enumerator.MoveNext()) continue;

                    var vLocation = graph.GetVertex(v);
                    var vTile = TileStatic.WorldTileLocalId(vLocation.longitude, vLocation.latitude, graph.Zoom);
                    if (vTile != tile) continue;

                    enumerator.MoveTo(v);
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Forward && enumerator.FaceRight != int.MaxValue) continue;
                        if (!enumerator.Forward && enumerator.FaceLeft != int.MaxValue) continue;

                        // ok this edge has an undetermined face.
                        var result = enumerator.AssignFace(unAssignableFace);
                        if (!result.success) return result;
                    }
                }
            }

            return (true, Enumerable.Empty<uint>());
        }

        internal static (IReadOnlyList<(int vertex1, int edge, bool forward, int vertex2)>? loop, IEnumerable<uint> missingTiles) RightTurnLoop(
            this TiledBarrierGraph.BarrierGraphEnumerator enumerator)
        {            
            var path = new List<(int v1, int e, bool f, int v2)>
                {(enumerator.Vertex1, enumerator.Edge, enumerator.Forward, enumerator.Vertex2)};

            // find a closed loop by always going right.
            var first = enumerator.NextRight();
            while (first != null)
            {
                // check for a u-turn.
                if (first.Vertex2 == path[^1].v1) break;

                if (!enumerator.Graph.HasTileFor(first.Vertex2))
                {
                    return (null, new []{ enumerator.Graph.TileFor(first.Vertex2) });
                }

                path.Add((first.Vertex1, first.Edge, first.Forward, first.Vertex2));

                if (first.Vertex2 == path[0].v1) break;

                first = first.NextRight();
            }

            return (path, Enumerable.Empty<uint>());
        }

        private static (bool success, IEnumerable<uint> missingTiles) AssignFace(this TiledBarrierGraph.BarrierGraphEnumerator enumerator,
            int unAssignableFace)
        {
            var (loop, missingTiles) = enumerator.RightTurnLoop();
            if (loop == null) return (false, missingTiles);

            var face = unAssignableFace;
            if (loop[0].vertex1 == loop[^1].vertex2)
            {
                // if loop is closed then we have a new face.
                face = enumerator.Graph.AddFace();
            }

            // set face.
            foreach (var (_, e, f, _) in loop)
            {
                var left = !f;
                enumerator.Graph.SetFace(e, left, face);
            }

            return (true, Enumerable.Empty<uint>());
        }

        public static Feature? ToPolygon(this TiledBarrierGraph graph, int face)
        {
            var enumerator = graph.GetFaceEnumerator();
            if (!enumerator.MoveTo(face)) return null;
            if (face == 0) return null;

            var edges = new List<(int vertex1, int edge, bool forward, int vertex2, (double longitude, double latitude)[] shape)>();
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

            if (edges.Count <= 1) return null;
            if (edges[0].vertex1 == edges[1].vertex2) edges.Reverse();
            if (edges[0].vertex1 != edges[^1].vertex2) return null;
            
            var coordinates = new List<Coordinate>();
            foreach (var edge in edges)
            {
                if (coordinates.Count == 0)
                {
                    var v1Location = graph.GetVertex(edge.vertex1);
                    coordinates.Add(new Coordinate(v1Location.longitude, v1Location.latitude));
                }

                for (var s = 0; s < edge.shape.Length; s++)
                {
                    var i = s;
                    if (!edge.forward) i = edge.shape.Length - i - 1;
                    var sp = edge.shape[i];
                    
                    coordinates.Add(new Coordinate(sp.longitude, sp.latitude));
                }
                
                var v2Location = graph.GetVertex(edge.vertex2);
                coordinates.Add(new Coordinate(v2Location.longitude, v2Location.latitude));
            }
            
            return new Feature(new NetTopologySuite.Geometries.Polygon(new LinearRing(coordinates.ToArray())), new AttributesTable {{"face", face}});
        }
    }
}