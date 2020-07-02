using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using OsmSharp;
using OsmSharp.Tags;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Faces;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Serialization;
using ANYWAYS.UrbanisticPolygons.Landuse;
using ANYWAYS.UrbanisticPolygons.Tiles;
using NetTopologySuite.Geometries;
using OsmSharp.Logging;

[assembly:InternalsVisibleTo("ANYWAYS.UrbanisticPolygons.Tests.Functional")]
namespace ANYWAYS.UrbanisticPolygons
{
    public static class TiledBarrierGraphBuilder
    {
        private static readonly ConcurrentDictionary<uint, uint> _tiles = new ConcurrentDictionary<uint, uint>();

        public static async Task BuildForTile(uint tile, string folder, Func<uint, IEnumerable<OsmGeo>> getTile,
            Func<TagsCollectionBase, bool> isBarrier)
        {
            // wait until tile is removed from queue.
            while (true)
            {
                if (_tiles.ContainsKey(tile))
                {
                    await Task.Delay(200);
                }
                else
                {
                    _tiles[tile] = tile;
                    break;
                }
            }
            
            try
            {
                var file = Path.Combine(folder, $"{tile}.tile.graph.zip");
                if (File.Exists(file)) return;

                // load data for tile.
                var graph = new TiledBarrierGraph();
                graph.LoadForTile(tile, getTile, isBarrier);

                // run face assignment for the tile.
                var result = graph.AssignFaces(tile);
                while (!result.success)
                {
                    // extra tiles need loading.-
                    graph.AddTiles(result.missingTiles, getTile, isBarrier);

                    // try again.
                    result = graph.AssignFaces(tile);
                }

                // assign landuse.
                IEnumerable<(Polygon polygon, string type)> GetLanduse(
                    ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box)
                {
                    return LandusePolygons.GetLandusePolygons(box, graph.Zoom, getTile, t =>
                    {
                        if (DefaultMergeFactorCalculator.Landuses.TryCalculateValue(t, out var type)) return type;

                        return null;
                    });
                }

                graph.AssignLanduse(tile, GetLanduse);

                await using var stream = File.Open(file, FileMode.Create);
                await using var compressedStream = new GZipStream(stream, CompressionLevel.Fastest);
                graph.WriteTileTo(compressedStream, tile);
            }
            finally
            {
                _tiles.Remove(tile, out _);
            }
        }

        internal static void LoadForTile(this TiledBarrierGraph graph, uint tile,
            Func<uint, IEnumerable<OsmGeo>> getTile,
            Func<TagsCollectionBase, bool> isBarrier)
        {
            // mark tile as loaded.
            graph.SetTileLoaded(tile);
            
            // first load the tile in question.
            var tileData = getTile(tile);
            var extraTiles = graph.Add(tileData, isBarrier);
            
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
            foreach (var tile in tiles)
            {
                // mark tile as loaded.
                graph.SetTileLoaded(tile);
                
                // get the data and load it.
                var tileData = getTile(tile);
                 graph.Add(tileData, isBarrier);
            }
            
            // prune graph.
            graph.PruneDeadEnds();
            graph.PruneShapePoints();
            
            // standardize edges
            // loading them in a different order could lead to different start and end points
            graph.StandardizeEdges();
        }
        
        private static IEnumerable<uint> Add(this TiledBarrierGraph graph, IEnumerable<OsmGeo> osmGeos,
            Func<TagsCollectionBase, bool> isBarrier)
        {
            var uncoveredTiles = new HashSet<uint>();
            
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

                var tile = TileStatic.WorldTileLocalId(node.Longitude.Value, node.Latitude.Value, graph.Zoom);
                if (!vertexNodes.ContainsKey(node.Id.Value) && 
                    graph.HasTile(tile))
                    continue; // node is not a vertex and inside a loaded tile.

                var vertex = graph.AddVertex(node.Longitude.Value, node.Latitude.Value, node.Id.Value);
                vertexNodes[node.Id.Value] = vertex;

                if (!graph.HasTile(tile)) uncoveredTiles.Add(tile);
            }
            
            // add all edges.
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
                            OsmSharp.Logging.Logger.Log(nameof(TiledBarrierGraphBuilder), TraceEventType.Warning,
                            $"Node location for node {node} in way {way.Id} not found!");
                        }
                        else
                        {
                            shape.Add(nodeLocation.Value);
                        }
                        continue;
                    }
                    else if (vertex == int.MaxValue)
                    {
                        OsmSharp.Logging.Logger.Log(nameof(TiledBarrierGraphBuilder), TraceEventType.Warning,
                            $"Node {node} in way {way.Id} not found in tile!");
                        continue;
                    }

                    if (vertex1 == int.MaxValue)
                    {
                        vertex1 = vertex;
                        continue;
                    }

                    graph.AddEdgeFlattened(vertex1, vertex, shape, way.Tags, way.Id.Value);
                    vertex1 = vertex;
                    shape.Clear();
                }
                
                hasNext = enumerator.MoveNext();
            }

            return uncoveredTiles;
        }
    }
}