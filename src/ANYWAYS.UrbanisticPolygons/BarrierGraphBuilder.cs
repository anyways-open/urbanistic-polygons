using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using OsmSharp;
using OsmSharp.Tags;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using ANYWAYS.UrbanisticPolygons.Tiles;
using OsmSharp.Logging;

[assembly:InternalsVisibleTo("ANYWAYS.UrbanisticPolygons.Tests.Functional")]
namespace ANYWAYS.UrbanisticPolygons
{
    internal static class BarrierGraphBuilder
    {
        internal static void LoadForTile(this TiledBarrierGraph graph, uint tile,
            Func<uint, IEnumerable<OsmGeo>> getTile,
            Func<TagsCollectionBase, bool> isBarrier)
        {
            // mark tile as loaded.
            graph.SetTileLoaded(tile);
            
            // first load the tile in question.
            var tileData = getTile(tile);
            graph.AddNonPlanar(tileData, isBarrier);
            
            // load other tiles until all edges with at least one vertex in the request tile are fully loaded.
            var extraTiles = new HashSet<uint>();
            var enumerator = graph.GetEnumerator();
            for (var v = 0; v < graph.VertexCount; v++)
            {
                if (!enumerator.MoveTo(v)) continue;

                while (enumerator.MoveNext())
                {
                    foreach (var edgeTile in enumerator.GetTiles())
                    {
                        if (edgeTile == tile) continue;

                        extraTiles.Add(edgeTile);
                    }
                }
            }
            
            // add all the tiles.
            graph.AddTiles(extraTiles, getTile, isBarrier);
        }

        internal static void AddTileFor(this TiledBarrierGraph graph, int vertex, Func<uint, IEnumerable<OsmGeo>> getTile,
            Func<TagsCollectionBase, bool> isBarrier)
        {
            var vLocation = graph.GetVertex(vertex);
            var t = TileStatic.WorldTileLocalId(vLocation.longitude, vLocation.latitude, graph.Zoom);
            graph.AddTiles(new[] {t}, getTile, isBarrier);
        }
        
        internal static void AddTiles(this TiledBarrierGraph graph, IEnumerable<uint> tiles,
            Func<uint, IEnumerable<OsmGeo>> getTile,
            Func<TagsCollectionBase, bool> isBarrier)
        {
            // load other tiles.
            var newEdges = new HashSet<int>();
            foreach (var tile in tiles)
            {
                // mark tile as loaded.
                graph.SetTileLoaded(tile);
                
                // get the data and load it.
                var tileData = getTile(tile);
                var extraTileNewEdges = graph.AddNonPlanar(tileData, isBarrier);
                
                // keep new edges.
                newEdges.UnionWith(extraTileNewEdges);
            }
            
            // flatten the graph.
            graph.Flatten();
            
            // prune graph.
            graph.PruneDeadEnds();
            graph.PruneShapePoints();
        }
        
        private static IEnumerable<int> AddNonPlanar(this TiledBarrierGraph graph, IEnumerable<OsmGeo> osmGeos,
            Func<TagsCollectionBase, bool> isBarrier)
        {
            // collect all nodes with more than one barrier way.
            var nodes = new Dictionary<long, (double longitude, double latitude)?>();
            var vertexNodes = new Dictionary<long, int>();
            foreach (var osmGeo in osmGeos)
            {
                if (!(osmGeo is Way way)) continue;
                if (way.Nodes == null) continue;
                if (!isBarrier(way.Tags)) continue;

                for (var n = 0; n < way.Nodes.Length; n++)
                {
                    var nodeId = way.Nodes[n];

                    if (graph.TryGetVertex(nodeId, out var vertex))
                    {
                        // node already there as a vertex.
                        vertexNodes[nodeId] = vertex;
                    }
                    else
                    {
                        // not yet a vertex.
                        // keep first, last and reused nodes are intersections.
                        if (n == 0 || n == way.Nodes.Length - 1 || 
                            nodes.ContainsKey(nodeId))
                        {
                            vertexNodes[nodeId] = int.MaxValue;
                        }
                    }

                    nodes[nodeId] = null;
                }
            }
            
            // add all vertices new vertices and store node locations.
            using var enumerator = osmGeos.GetEnumerator();
            var hasNext = true;
            while (hasNext)
            {
                hasNext = enumerator.MoveNext();
                if (!hasNext) break;
                
                if (!(enumerator.Current is Node node)) break;
                if (node.Id == null || node.Latitude == null || node.Longitude == null) continue;
                if (graph.TryGetVertex(node.Id.Value, out _)) continue;
                if (!nodes.ContainsKey(node.Id.Value)) continue; // not part of a barrier way.
                
                nodes[node.Id.Value] = (node.Longitude.Value, node.Latitude.Value);

                if (!vertexNodes.ContainsKey(node.Id.Value) && 
                    graph.HasTile(TileStatic.WorldTileLocalId(node.Longitude.Value, node.Latitude.Value, graph.Zoom)))
                    continue; // node is not a vertex and inside a loaded tile.

                var vertex = graph.AddVertex(node.Longitude.Value, node.Latitude.Value, node.Id.Value);
                vertexNodes[node.Id.Value] = vertex;
            }
            
            // add all edges.
            var edges = new List<int>();
            var shape = new List<(double longitude, double latitude)>();
            while (hasNext)
            {
                if (!hasNext) break;
                
                if (!(enumerator.Current is Way way)) break;
                if (way.Nodes == null || way.Tags == null || way.Id == null)
                {
                    hasNext = enumerator.MoveNext();
                    continue;
                }
                if (!isBarrier(way.Tags)) 
                {
                    hasNext = enumerator.MoveNext();
                    continue;
                }
                if (graph.HasWay(way.Id.Value)) 
                {
                    hasNext = enumerator.MoveNext();
                    continue;
                }
                
                // way is a barrier, add it as one or more edges.
                shape.Clear();
                var vertex1 = int.MaxValue;
                foreach (var node in way.Nodes)
                {
                    if (!vertexNodes.TryGetValue(node, out var vertex))
                    {
                        if (!nodes.TryGetValue(node, out var nodeLocation)) throw new InvalidDataException(
                            $"Node {node} in way {way.Id} not found!");
                        if (nodeLocation == null) 
                        {
                            OsmSharp.Logging.Logger.Log(nameof(BarrierGraphBuilder), TraceEventType.Warning,
                            $"Node location for node {node} in way {way.Id} not found!");
                        }
                        else
                        {
                            shape.Add(nodeLocation.Value);
                        }
                        continue;
                    }

                    if (vertex1 == int.MaxValue)
                    {
                        vertex1 = vertex;
                        continue;
                    }

                    edges.Add(graph.AddEdge(vertex1, vertex, shape, way.Tags, way.Id.Value));
                    vertex1 = vertex;
                    shape.Clear();
                }
                
                hasNext = enumerator.MoveNext();
            }

            return edges;
        }
    }
}