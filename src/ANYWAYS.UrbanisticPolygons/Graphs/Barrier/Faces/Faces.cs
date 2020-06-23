using System;
using System.Collections.Generic;
using ANYWAYS.UrbanisticPolygons.Tiles;
using OsmSharp;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Faces
{
    internal static class Faces
    {
        public static void AssignFaces(this TiledBarrierGraph graph, uint tile, Func<uint, IEnumerable<OsmGeo>> getTile,
            Func<TagsCollectionBase, bool> isBarrier)
        {
            if (!graph.HasTile(tile)) graph.LoadForTile(tile, getTile, isBarrier);

            var enumerator = graph.GetEnumerator();
            for (var v = 0; v < graph.VertexCount; v++)
            {
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
                    if (!enumerator.AssignFace(getTile, isBarrier))
                    {
                        // redo vertex, assignment failed, graph was incomplete.
                        enumerator.MoveTo(v);
                    }
                }
            }
        }

        private static bool AssignFace(this TiledBarrierGraph.BarrierGraphEnumerator enumerator,
            Func<uint, IEnumerable<OsmGeo>> getTile,
            Func<TagsCollectionBase, bool> isBarrier)
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
                    // load tile and restart.
                    enumerator.Graph.AddTileFor(first.Vertex2, getTile, isBarrier);

                    return false;
                }

                path.Add((first.Vertex1, first.Edge, first.Forward, first.Vertex2));

                if (first.Vertex2 == path[0].v1) break;

                first = first.NextRight();
            }

            // if loop is closed then we have a new face.
            if (path[0].v1 == path[^1].v2)
            {
                var face = enumerator.Graph.AddFace();

                // set face.
                for (var i = 0; i < path.Count; i++)
                {
                    var (_, e, f, _) = path[i];

                    var left = !f;
                    enumerator.Graph.SetFace(e, left, face);
                }
            }

            return true;
        }
    }
}