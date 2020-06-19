using System;
using System.Collections.Generic;
using System.Linq;
using ANYWAYS.UrbanisticPolygons.Tiles;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using OsmSharp.Geo;

namespace ANYWAYS.UrbanisticPolygons.Graphs.Barrier
{
    internal static class TiledBarrierGraphExtensions
    {
        private static readonly RobustLineIntersector Intersector = new RobustLineIntersector();
        
        internal static void Flatten(this TiledBarrierGraph graph, IEnumerable<int>? newEdges = null)
        {
            HashSet<int>? edgeToCheck = null;
            if (newEdges != null) edgeToCheck = new HashSet<int>(newEdges);
            
            var edgeEnumerator1 = graph.GetEnumerator();
            var edgeEnumerator2 = graph.GetEnumerator();
            for (var v1 = 0; v1 < graph.VertexCount; v1++)
            {
                var split = false;
                for (var v2 = v1 + 1; v2 < graph.VertexCount; v2++)
                {
                    if (split) break;
                    if (!edgeEnumerator1.MoveTo(v1)) continue;

                    while (edgeEnumerator1.MoveNext())
                    {
                        if (split) break;
                        if (!edgeEnumerator1.Forward) continue; // only consider forward directions
                        
                        if (edgeToCheck != null && !edgeToCheck.Contains(edgeEnumerator1.Edge)) continue;
                        if (!edgeEnumerator2.MoveTo(v2)) continue;

                        while (edgeEnumerator2.MoveNext())
                        {
                            if (!edgeEnumerator2.Forward) continue; // only consider forward directions
                            
                            // intersect here and use the first result.
                            var intersectionResult = edgeEnumerator1.Intersect(edgeEnumerator2);

                            // if intersection found:
                            // - split edges
                            // - restart at v1.
                            if (intersectionResult == null) continue;
                            var intersection = intersectionResult.Value;
                            
                            // get shapes.
                            var shape11 = edgeEnumerator1.ShapeTo(intersection.shape1);
                            var shape12 = edgeEnumerator1.ShapeFrom(intersection.shape1);
                            var shape21 = edgeEnumerator2.ShapeTo(intersection.shape2);
                            var shape22 = edgeEnumerator2.ShapeFrom(intersection.shape2);
                            
                            // add new vertex.
                            var vertex = graph.AddVertex(intersection.longitude,
                                intersection.latitude);
                                
                            // add 4 new edges.
                            
                            // edge1 vertex1 -> vertex
                            graph.AddEdge(edgeEnumerator1.Vertex1, vertex, shape11, edgeEnumerator1.Tags);
                            // vertex -> edge1 vertex2
                            graph.AddEdge(vertex, edgeEnumerator1.Vertex2, shape12, edgeEnumerator1.Tags);

                            // edge2 vertex1 -> vertex
                            graph.AddEdge(edgeEnumerator2.Vertex1, vertex, shape21, edgeEnumerator2.Tags);
                            // vertex -> edge2 vertex2
                            graph.AddEdge(vertex, edgeEnumerator2.Vertex2, shape22, edgeEnumerator2.Tags);
                            
                            // remove original edges.
                            graph.DeleteEdge(edgeEnumerator1.Edge);
                            graph.DeleteEdge(edgeEnumerator2.Edge);

                            split = true;
                            break;
                        }
                    }
                }

                if (split) v1--;
            }
        }

        internal static void PruneShapePoints(this TiledBarrierGraph graph)
        {
            var enumerator = graph.GetEnumerator();
            for (var v = 0; v < graph.VertexCount; v++)
            {
                if (!enumerator.MoveTo(v)) continue;
                if (!enumerator.MoveNext()) continue;
                if (!enumerator.MoveNext()) continue;
                if (enumerator.MoveNext()) continue;

                // vertex has exactly two neighbours.
                // check if they match.
                enumerator.MoveTo(v);
                enumerator.MoveNext();
                var vertex1 = enumerator.Vertex2;
                var edge1 = enumerator.Edge;
                var tags1 = enumerator.Tags;
                var shape1 = enumerator.CompleteShape().ToList();
                if (!enumerator.IsInLoadedTile()) continue;
                enumerator.MoveNext();
                var vertex2 = enumerator.Vertex2;
                var edge2 = enumerator.Edge;
                var tags2 = enumerator.Tags;
                var shape2 = enumerator.CompleteShape().ToList();
                if (!tags1.Equals(tags2)) continue;
                if (!enumerator.IsInLoadedTile()) continue;

                // both have identical tags and are completely in a loaded tile.

                // add a new edge.
                var shape = new List<(double longitude, double latitude)>();
                shape1.Reverse();
                shape.AddRange(shape1.GetRange(1, shape1.Count - 2));
                shape.Add(graph.GetVertex(v));
                shape.AddRange(shape2.GetRange(1, shape2.Count - 2));
                graph.AddEdge(vertex1, vertex2, shape, tags1);

                // remove old edges.
                graph.DeleteEdge(edge1);
                graph.DeleteEdge(edge2);
            }
        }
        
        internal static void PruneDeadEnds(this TiledBarrierGraph graph)
        {
            var queue = new Queue<int>();
            
            var enumerator = graph.GetEnumerator();
            for (var v = 0; v < graph.VertexCount; v++)
            {
                if (!enumerator.MoveTo(v)) continue;
                if (!enumerator.MoveNext()) continue;
                if (enumerator.MoveNext()) continue;
                
                // vertex has only one neighbour.
                enumerator.MoveTo(v);
                enumerator.MoveNext();
                
                if (enumerator.Vertex2 == v) continue; // we leave in the tiny one-edge sized islands.
                if (!enumerator.IsInLoadedTile()) continue; // we cannot prune edges that may be incomplete.
                
                if (enumerator.Vertex2 < v) queue.Enqueue(enumerator.Vertex2);
                graph.DeleteEdge(enumerator.Edge);
            }

            while (queue.Count > 0)
            {
                var v = queue.Dequeue();
                
                if (!enumerator.MoveTo(v)) continue;
                if (!enumerator.MoveNext()) continue;
                if (enumerator.MoveNext()) continue;
                
                // vertex has only one neighbour.
                enumerator.MoveTo(v);
                enumerator.MoveNext();
                
                if (enumerator.Vertex2 == v) continue; // we leave in the tiny one-edge sized islands.
                if (!enumerator.IsInLoadedTile()) continue; // we cannot prune edges that may be incomplete.
                
                queue.Enqueue(enumerator.Vertex2);
                graph.DeleteEdge(enumerator.Edge);
            }
        }

        internal static IEnumerable<uint> GetTiles(this TiledBarrierGraph.BarrierGraphEnumerator enumerator)
        {
            var zoom = enumerator.Graph.Zoom;
            foreach (var location in enumerator.CompleteShape())
            {
                var locationTile = TileStatic.WorldToTile(location.longitude, location.latitude, zoom);
                yield return TileStatic.ToLocalId(locationTile, zoom);
            }
        }
        
        internal static bool IsInLoadedTile(this TiledBarrierGraph.BarrierGraphEnumerator enumerator)
        {
            var graph = enumerator.Graph;
            foreach (var tile in enumerator.GetTiles())
            {
                if (!graph.HasTile(tile)) return false;
            }
            return true;
        }

        internal static IEnumerable<(double longitude, double latitude)> CompleteShape(
            this TiledBarrierGraph.BarrierGraphEnumerator enumerator)
        {
            yield return enumerator.Graph.GetVertex(enumerator.Vertex1);
            
            for (var s = 0; s < enumerator.Shape.Length; s++)
            {
                var i = s;
                if (!enumerator.Forward)
                {
                    i = enumerator.Shape.Length - s - 1;
                }

                var sp = enumerator.Shape[i];
                yield return sp;
            }
            
            yield return enumerator.Graph.GetVertex(enumerator.Vertex2);
        }

        internal static IEnumerable<(((double longitude, double latitude) coordinate1,
            (double longitude, double latitude) coordinate2) line, int index)> Segments(
            this TiledBarrierGraph.BarrierGraphEnumerator enumerator)
        {
            using var shapePoints = enumerator.CompleteShape().GetEnumerator();
            shapePoints.MoveNext();
            var location1 = shapePoints.Current;
            shapePoints.MoveNext();
            var location2 = shapePoints.Current;
            var i = 0;

            yield return ((location1, location2), i);
            while (shapePoints.MoveNext())
            {
                location1 = location2;
                location2 = shapePoints.Current;
                i++;
                
                yield return ((location1, location2), i);
            }
        }

        internal static (double longitude, double latitude, int shape1, int shape2)? Intersect(
            this TiledBarrierGraph.BarrierGraphEnumerator enumerator1,
            TiledBarrierGraph.BarrierGraphEnumerator enumerator2)
        {
            foreach (var segment1 in enumerator1.Segments())
            foreach (var segment2 in enumerator2.Segments())
            {
                Intersector.ComputeIntersection(
                    new Coordinate(segment1.line.coordinate1.longitude, segment1.line.coordinate1.latitude),
                    new Coordinate(segment1.line.coordinate2.longitude, segment1.line.coordinate2.latitude),
                    new Coordinate(segment2.line.coordinate1.longitude, segment2.line.coordinate1.latitude),
                    new Coordinate(segment2.line.coordinate2.longitude, segment2.line.coordinate2.latitude));
                if (Intersector.HasIntersection &&
                    Intersector.IsProper)
                {
                    var intersection = Intersector.GetIntersection(0);
                    return (intersection.X, intersection.Y, segment1.index, segment2.index);
                }
            }

            return null;
        }

        internal static IEnumerable<(double longitude, double latitude)> ShapeTo(
            this TiledBarrierGraph.BarrierGraphEnumerator enumerator, int index)
        {
            if (index == 0) yield break;

            for (var s = 0; s < enumerator.Shape.Length; s++)
            {
                if (s < index) yield return enumerator.Shape[s];
            }
        }

        internal static IEnumerable<(double longitude, double latitude)> ShapeFrom(
            this TiledBarrierGraph.BarrierGraphEnumerator enumerator, int index)
        {
            for (var s = 0; s < enumerator.Shape.Length; s++)
            {
                if (s >= index) yield return enumerator.Shape[s];
            }
        }

        internal static IEnumerable<Feature> ToFeatures(this TiledBarrierGraph graph)
        {
            foreach (var tileFeature in graph.ToTileFeatures())
            {
                yield return tileFeature;
            }
            
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
                    var attributes = enumerator.Tags.ToAttributeTable();

                    yield return new Feature(lineString, attributes);
                }
                if (hasEdge) yield return new Feature(graph.ToPoint(v), new AttributesTable {{"vertex", v}});
            }
        }

        private static IEnumerable<Feature> ToTileFeatures(this TiledBarrierGraph graph)
        {
            foreach (var tile in graph.LoadedTiles())
            {
                var box = TileStatic.Box(graph.Zoom, tile);
                var polygon = new NetTopologySuite.Geometries.Polygon(new LinearRing(new []
                {
                    new Coordinate(box.left, box.top), 
                    new Coordinate(box.right, box.top), 
                    new Coordinate(box.right, box.bottom), 
                    new Coordinate(box.left, box.bottom), 
                    new Coordinate(box.left, box.top)
                }));
            
                yield return new Feature(polygon, new AttributesTable{{"tile_id", tile},{"zoom", graph.Zoom}});
            }
        }

        internal static Point ToPoint(this TiledBarrierGraph graph, int vertex)
        {
            var location = graph.GetVertex(vertex);
            return new Point(new Coordinate(location.longitude, location.latitude));
        }
        
        internal static LineString ToLineString(this TiledBarrierGraph.BarrierGraphEnumerator enumerator)
        {
            var coordinates = new Coordinate[enumerator.Shape.Length + 2];

            var vertex1Location = enumerator.Graph.GetVertex(enumerator.Vertex1);
            coordinates[0] = new Coordinate(vertex1Location.longitude, vertex1Location.latitude);

            for (var s = 0; s < enumerator.Shape.Length; s++)
            {
                var i = s;
                if (!enumerator.Forward)
                {
                    i = enumerator.Shape.Length - s;
                }

                var sp = enumerator.Shape[i];
                coordinates[i + 1] = new Coordinate(sp.longitude, sp.latitude);
            }

            var vertex2Location = enumerator.Graph.GetVertex(enumerator.Vertex2);
            coordinates[^1] = new Coordinate(vertex2Location.longitude, vertex2Location.latitude);
            
            return new LineString(coordinates);
        }
    }
}