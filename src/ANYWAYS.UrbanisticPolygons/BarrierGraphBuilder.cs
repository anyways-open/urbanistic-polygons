using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using OsmSharp;
using OsmSharp.Tags;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using ANYWAYS.UrbanisticPolygons.Tiles;

[assembly:InternalsVisibleTo("ANYWAYS.UrbanisticPolygons.Tests.Functional")]
namespace ANYWAYS.UrbanisticPolygons
{
    internal static class BarrierGraphBuilder
    {
        internal static void AddTile(this TiledBarrierGraph graph, uint tile, IEnumerable<OsmGeo> osmGeos,
            Func<TagsCollectionBase, bool> isBarrier)
        {
            // build 'isVertex' function.
            // force nodes outside the current tile to be vertices.
            // we don't know yet what intersections may exist.
            bool IsVertex((double longitude, double latitude) location)
            {
                var (x, y) = TileStatic.WorldToTile(location.longitude, location.latitude, 
                    graph.Zoom);
                return TileStatic.ToLocalId(x, y, graph.Zoom) != tile;
            }
            
            // add the data.
            graph.AddFrom(osmGeos, isBarrier, IsVertex);
            
            // mark tile as loaded.
            graph.SetTileLoaded(tile);
            
            // prune graph.
            graph.PruneDeadEnds();
        }
        
        private static void AddFrom(this TiledBarrierGraph graph, IEnumerable<OsmGeo> osmGeos,
            Func<TagsCollectionBase, bool> isBarrier, Func<(double longitude, double latitude), bool> isVertex = null)
        {
            // add the new data without flattening.
            var newEdges = graph.AddNonPlanar(osmGeos, isBarrier, isVertex);
            
            // flatten graph.
            graph.Flatten(newEdges);
        }
        
        private static IEnumerable<int> AddNonPlanar(this TiledBarrierGraph graph, IEnumerable<OsmGeo> osmGeos,
            Func<TagsCollectionBase, bool> isBarrier, Func<(double longitude, double latitude), bool> isVertex = null)
        {
            // collect all nodes with more than one barrier way.
            var nodes = new Dictionary<long, (double longitude, double latitude)>();
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

                    nodes[nodeId] = (double.MaxValue, double.MaxValue);
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
                
                var nodeLocation = (node.Longitude.Value, node.Latitude.Value);
                nodes[node.Id.Value] = nodeLocation;
                
                if (!vertexNodes.ContainsKey(node.Id.Value) && !isVertex(nodeLocation)) continue;

                vertexNodes[node.Id.Value] = graph.AddVertex(node.Longitude.Value, node.Latitude.Value);
            }
            
            // add all edges.
            var edges = new List<int>();
            var shape = new List<(double longitude, double latitude)>();
            while (hasNext)
            {
                if (!hasNext) break;
                
                Console.WriteLine($"Processing: {enumerator.Current}");
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
                        shape.Add(nodeLocation);
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