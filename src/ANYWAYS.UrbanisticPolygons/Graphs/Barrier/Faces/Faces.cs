using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ANYWAYS.UrbanisticPolygons.Geo;
using ANYWAYS.UrbanisticPolygons.Guids;
using ANYWAYS.UrbanisticPolygons.Tiles;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using OsmSharp.Logging;

[assembly:InternalsVisibleTo("ANYWAYS.UrbanisticPolygons.Tests.Functional")]
namespace ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Faces
{
    internal static class Faces
    {
        public static (bool success, IEnumerable<uint> missingTiles) AssignFaces(this TiledBarrierGraph graph,
            uint tile)
        {
            if (!graph.HasTile(tile)) return (false, new[] {tile});
            var tileBox = TileStatic.Box(graph.Zoom, tile);

            var tilesMissing = new HashSet<uint>();
            graph.ResetFaces();

            // the default face for the case where a loop cannot be found.
            var unAssignableFace = graph.AddFace();
            
            // check each edges for faces and if missing assign them.
            var enumerator = graph.GetEnumerator();
            for (var v = 0; v < graph.VertexCount; v++)
            {
                if (!enumerator.MoveTo(v)) continue;
                if (!enumerator.MoveNext()) continue;

                var vBox = graph.GetVertexBox(v);
                if (vBox == null || !vBox.Value.Overlaps(tileBox)) continue;
                // var vTile = TileStatic.WorldTileLocalId(vLocation.longitude, vLocation.latitude, graph.Zoom);
                // if (vTile != tile) continue;

                enumerator.MoveTo(v);
                while (enumerator.MoveNext())
                {
                    if (enumerator.Forward && enumerator.FaceRight != int.MaxValue) continue;
                    if (!enumerator.Forward && enumerator.FaceLeft != int.MaxValue) continue;
                    
                    // check if the edge bbox overlaps the tiles.
                    var eBox = enumerator.Box;
                    if (!eBox.Overlaps(tileBox)) continue;

                    // ok this edge has an undetermined face.
                    var result = enumerator.AssignFace(unAssignableFace);
                    if (!result.success)
                    {
                        tilesMissing.UnionWith(result.missingTiles);
                    }
                }
            }

            if (tilesMissing.Count > 0)
            {
                return (false, tilesMissing);
            }

            return (true, Enumerable.Empty<uint>());
        }

        internal static (IReadOnlyList<(int vertex1, int edge, bool forward, int vertex2)>? loop, IEnumerable<uint> missingTiles) RightTurnLoop(
            this TiledBarrierGraph.BarrierGraphEnumerator enumerator)
        {            
            var edges = new HashSet<(int e, bool dir)>();
            edges.Add((enumerator.Edge, enumerator.Forward));
            var path = new List<(int v1, int e, bool f, int v2)>
                {(enumerator.Vertex1, enumerator.Edge, enumerator.Forward, enumerator.Vertex2)};

            // find a closed loop by always going right.
            var first = enumerator.NextRight();
            while (first != null)
            {
                // check for a u-turn.
                if (first.Edge == path[^1].e) break;

                if (!enumerator.Graph.HasTileFor(first.Vertex2))
                {
                    return (null, new []{ enumerator.Graph.TileFor(first.Vertex2) });
                }

                path.Add((first.Vertex1, first.Edge, first.Forward, first.Vertex2));
                if (edges.Contains((first.Edge, first.Forward)))
                {
                    OsmSharp.Logging.Logger.Log(nameof(TiledBarrierGraphBuilder), TraceEventType.Warning,
                        $"Edge visited twice in same direction!");
                    return (null, Enumerable.Empty<uint>());
                } 
                edges.Add((first.Edge, first.Forward));

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
                // remove parts that go over the same edge twice.
                var removed = true;
                while (removed)
                {
                    removed = false;
                    var edges = new Dictionary<int, int>();
                    for (var i = 0; i < loop.Count; i++)
                    {
                        var e = loop[i].edge;
                        if (edges.TryGetValue(e, out var fi))
                        {
                            var copy = new List<(int vertex1, int edge, bool forward, int vertex2)>(loop);
                            
                            // a duplicate was detected.
                            copy.RemoveRange(fi, i - fi + 1);
                            loop = copy;
                            
                            removed = true;
                            break;
                        }

                        edges.Add(e, i);
                    }
                }

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

        public static
            IEnumerable<(int vertex1, int edge, bool forward, int vertex2, (double longitude, double latitude)[] shape)>
            EnumerateFaceClockwise(
                this TiledBarrierGraph graph, int face, int maxFaceCount = ushort.MaxValue)
        {
            var enumerator = graph.GetFaceEnumerator();
            if (!enumerator.MoveTo(face)) yield break;
            if (face == 0) yield break;
            
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

                if (edges.Count > maxFaceCount)
                {
                    yield break;
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

        public static IEnumerable<(double longitude, double latitude)> FaceToClockwiseCoordinates(
            this TiledBarrierGraph graph, int face)
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

        public static Polygon? ToPolygon(this TiledBarrierGraph graph, int face)
        {
            var coordinates = new List<Coordinate>();
            foreach (var c in graph.FaceToClockwiseCoordinates(face))
            {
                coordinates.Add(new Coordinate(c.longitude, c.latitude));
            }

            if (coordinates.Count <= 3) return null;

            return new Polygon(new LinearRing(coordinates.ToArray()));
        }

        public static Feature? ToPolygonFeature(this TiledBarrierGraph graph, int face)
        {
            var coordinates = new List<Coordinate>();
            foreach (var c in graph.FaceToClockwiseCoordinates(face))
            {
                coordinates.Add(new Coordinate(c.longitude, c.latitude));
            }

            if (coordinates.Count <= 3) return null;

            var attributes = new AttributesTable {{"face", face}, {"face_guid", graph.GetFaceGuid(face)}};
            var faceData = graph.GetFaceData(face);
            foreach (var fa in faceData)
            {
                attributes.Add($"face_{fa.type}", fa.percentage);
            }
            return new Feature(new NetTopologySuite.Geometries.Polygon(new LinearRing(coordinates.ToArray())), 
                attributes);
        }
    }
}