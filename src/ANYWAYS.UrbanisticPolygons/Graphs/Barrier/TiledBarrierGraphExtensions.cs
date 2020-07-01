using System;
using System.Collections.Generic;
using System.Linq;
using ANYWAYS.UrbanisticPolygons.Geo;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Faces;
using ANYWAYS.UrbanisticPolygons.Guids;
using ANYWAYS.UrbanisticPolygons.Tiles;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using OsmSharp.Geo;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons.Graphs.Barrier
{
    internal static class TiledBarrierGraphExtensions
    {
        private static readonly RobustLineIntersector Intersector = new RobustLineIntersector();

        internal static bool HasTileFor(this TiledBarrierGraph graph, int vertex)
        {
            return graph.HasTile(graph.TileFor(vertex));
        }

        internal static uint TileFor(this TiledBarrierGraph graph, int vertex)
        {
            var l = graph.GetVertex(vertex);
            return TileStatic.WorldTileLocalId(l.longitude, l.latitude, graph.Zoom);
        }

        internal static void MoveNextUntil(this TiledBarrierGraph.BarrierGraphEnumerator enumerator, int edge)
        {
            while (enumerator.MoveNext())
            {
                if (enumerator.Edge == edge) break;
            }
        }

        internal static void AddEdgeFlattened(this TiledBarrierGraph graph, int vertex1, int vertex2, IEnumerable<(double longitude, double latitude)>? shape = null,
            TagsCollectionBase tags = null, long? way = null)
        {
            shape ??= Enumerable.Empty<(double longitude, double latitude)>();
            
            // add the edge first.
            var firstEdge = graph.AddEdge(vertex1, vertex2, shape, tags, way);
            var edges = new Dictionary<int, ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)>();
            var newEnumerator = graph.GetEnumerator();
            newEnumerator.MoveToEdge(firstEdge);
            edges[firstEdge] = newEnumerator.Box;
            var firstBox = newEnumerator.Box;
            
            // get all the vertices with an overlap.
            var vertices = graph.GetVerticesOverlapping(firstBox);

            // check all edges already in the graph against this new edge.
            var existingEnumerator = graph.GetEnumerator();
            foreach (var v in vertices)
            //for (var v = 0; v < graph.VertexCount; v++)
            {
                var split = true;
                while (split)
                {
                    split = false;
                    if (!existingEnumerator.MoveTo(v)) continue;

                    // check the vertex for quick negatives.
                    var vertexBox = graph.GetVertexBox(v);
                    if (!vertexBox.HasValue || !vertexBox.Value.Overlaps(firstBox)) continue;

                    while (existingEnumerator.MoveNext())
                    {
                        if (!existingEnumerator.Forward) continue; // only consider forward directions
                        if (edges.ContainsKey(existingEnumerator.Edge)) continue; // don't test self-intersections.

                        // check the first box for quick negatives.
                        var edgeBox = existingEnumerator.Box;
                        if (!edgeBox.Overlaps(firstBox)) continue;

                        foreach (var e in edges)
                        {
                            var box = e.Value;
                            if (!edgeBox.Overlaps(box)) continue;

                            // move to the edge, boxes overlap.
                            newEnumerator.MoveToEdge(e.Key);

                            // intersect here and use the first result.
                            var intersectionResult = existingEnumerator.Intersect(newEnumerator);

                            // if intersection found:
                            // - split edges
                            // - restart at v1.
                            if (intersectionResult == null) continue;
                            var intersection = intersectionResult.Value;
                            
                            // get shapes.
                            var shape11 = existingEnumerator.ShapeTo(intersection.shape1).ToArray();
                            var shape12 = existingEnumerator.ShapeFrom(intersection.shape1).ToArray();
                            var shape21 = newEnumerator.ShapeTo(intersection.shape2).ToArray();
                            var shape22 = newEnumerator.ShapeFrom(intersection.shape2).ToArray();

                            // add new vertex.
                            var vertex = graph.AddVertex(intersection.longitude,
                                intersection.latitude);
                            
                            // add 4 new edges.

                            // edge1 vertex1 -> vertex
                            graph.AddEdge(vertex, existingEnumerator.Vertex1, shape11.Reverse(), existingEnumerator.Tags);
                            // vertex -> edge1 vertex2
                            graph.AddEdge(vertex, existingEnumerator.Vertex2, shape12, existingEnumerator.Tags);

                            // edge2 vertex1 -> vertex
                            var newVertex1 = newEnumerator.Vertex1;
                            var newVertex2 = newEnumerator.Vertex2;
                            var e1 = graph.AddEdge(vertex, newVertex1, shape21.Reverse(), newEnumerator.Tags);
                            newEnumerator.MoveToEdge(e1);
                            edges[e1] = newEnumerator.Box;
                            // vertex -> edge2 vertex2
                            var e2 = graph.AddEdge(vertex, newVertex2, shape22, newEnumerator.Tags);
                            newEnumerator.MoveToEdge(e2);
                            edges[e2] = newEnumerator.Box;

                            // remove original edges.
                            graph.DeleteEdge(existingEnumerator.Edge);
                            graph.DeleteEdge(e.Key);
                            edges.Remove(e.Key);

                            split = true;
                            break;
                        }

                        if (split) break;
                    }
                }
            }
        }
        //
        // internal static void Flatten(this TiledBarrierGraph graph, IEnumerable<int>? newEdges = null)
        // {
        //     HashSet<int>? edgeToCheck = null;
        //     if (newEdges != null) edgeToCheck = new HashSet<int>(newEdges);
        //     
        //     var edgeEnumerator1 = graph.GetEnumerator();
        //     var edgeEnumerator2 = graph.GetEnumerator();
        //     for (var v1 = 0; v1 < graph.VertexCount; v1++)
        //     {
        //         var split = false;
        //         if (!edgeEnumerator1.MoveTo(v1)) continue;
        //         while (edgeEnumerator1.MoveNext())
        //         {
        //             if (split) break;
        //             if (!edgeEnumerator1.Forward) continue; // only consider forward directions
        //             if (edgeToCheck != null && !edgeToCheck.Contains(edgeEnumerator1.Edge)) continue;
        //             
        //             for (var v2 = 0; v2 < graph.VertexCount; v2++)
        //             {
        //                 if (split) break;
        //
        //                 if (!edgeEnumerator2.MoveTo(v2)) continue;
        //
        //                 var box1 = edgeEnumerator1.Box;
        //                 while (edgeEnumerator2.MoveNext())
        //                 {
        //                     if (!edgeEnumerator2.Forward) continue; // only consider forward directions
        //                     var box2 = edgeEnumerator2.Box;
        //                     if (!box1.Overlaps(box2)) continue;
        //
        //                     // intersect here and use the first result.
        //                     var intersectionResult = edgeEnumerator1.Intersect(edgeEnumerator2);
        //
        //                     // if intersection found:
        //                     // - split edges
        //                     // - restart at v1.
        //                     if (intersectionResult == null) continue;
        //                     var intersection = intersectionResult.Value;
        //
        //                     // get shapes.
        //                     var shape11 = edgeEnumerator1.ShapeTo(intersection.shape1);
        //                     var shape12 = edgeEnumerator1.ShapeFrom(intersection.shape1);
        //                     var shape21 = edgeEnumerator2.ShapeTo(intersection.shape2);
        //                     var shape22 = edgeEnumerator2.ShapeFrom(intersection.shape2);
        //
        //                     // add new vertex.
        //                     var vertex = graph.AddVertex(intersection.longitude,
        //                         intersection.latitude);
        //
        //                     // add 4 new edges.
        //
        //                     // edge1 vertex1 -> vertex
        //                     graph.AddEdge(edgeEnumerator1.Vertex1, vertex, shape11, edgeEnumerator1.Tags);
        //                     // vertex -> edge1 vertex2
        //                     graph.AddEdge(vertex, edgeEnumerator1.Vertex2, shape12, edgeEnumerator1.Tags);
        //
        //                     // edge2 vertex1 -> vertex
        //                     graph.AddEdge(edgeEnumerator2.Vertex1, vertex, shape21, edgeEnumerator2.Tags);
        //                     // vertex -> edge2 vertex2
        //                     graph.AddEdge(vertex, edgeEnumerator2.Vertex2, shape22, edgeEnumerator2.Tags);
        //
        //                     // remove original edges.
        //                     graph.DeleteEdge(edgeEnumerator1.Edge);
        //                     graph.DeleteEdge(edgeEnumerator2.Edge);
        //
        //                     split = true;
        //                     break;
        //                 }
        //             }
        //         }
        //
        //         if (split) v1--;
        //     }
        // }

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

        internal static void StandardizeEdges(this TiledBarrierGraph graph)
        {
            var enumerator = graph.GetEnumerator();
            for (var v = 0; v < graph.VertexCount; v++)
            {
                if (!enumerator.MoveTo(v)) continue;
                var v1Location = graph.GetVertex(v);

                while (enumerator.MoveNext())
                {
                    if (!enumerator.Forward) continue;

                    var v2Location = graph.GetVertex(enumerator.Vertex2);

                    if (v1Location.IsLeftOf(v2Location)) continue;

                    graph.ReverseEdge(enumerator.Edge);
                }
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

        internal static IEnumerable<(((double longitude, double latitude) coordinate1,
            (double longitude, double latitude) coordinate2) line, int index)> Segments(
            this (double longitude, double latitude)[] shape)
        {
            if (shape.Length < 2) yield break;
            var location1 = shape[0];
            var location2 = shape[1];
            yield return ((location1, location2), 0);
            for (var i = 2; i < shape.Length; i++)
            {
                location1 = location2;
                location2 = shape[i];
                
                yield return ((location1, location2), i - 1);
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

        internal static (double longitude, double latitude, int shape1, int shape2)? Intersect(
            this TiledBarrierGraph.BarrierGraphEnumerator enumerator1,
            (double longitude, double latitude)[] shape)
        {
            foreach (var segment1 in enumerator1.Segments())
            foreach (var segment2 in shape.Segments())
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

        internal static IEnumerable<(double longitude, double latitude)> ShapeTo(
            this (double longitude, double latitude)[] shape, int index)
        {
            if (index == 0) yield break;

            for (var s = 0; s < shape.Length; s++)
            {
                if (s < index) yield return shape[s];
            }
        }

        internal static IEnumerable<(double longitude, double latitude)> ShapeFrom(
            this (double longitude, double latitude)[] shape, int index)
        {
            for (var s = 0; s < shape.Length; s++)
            {
                if (s >= index) yield return shape[s];
            }
        }

        internal static IEnumerable<Feature> ToFeatures(this TiledBarrierGraph graph)
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
                yield return polygon;
            }
            
            foreach (var tileFeature in graph.ToTileFeatures())
            {
                yield return tileFeature;
            }
        }

        internal static IEnumerable<Feature> GetAllPolygons(this TiledBarrierGraph graph)
        {
            // for every face, determine polygon.
            for (var f = 0; f < graph.FaceCount; f++)
            {
                var polygon = graph.ToPolygonFeature(f);
                if (polygon == null) continue;
                
                yield return polygon;
            }
        }

        private static IEnumerable<Feature> ToTileFeatures(this TiledBarrierGraph graph)
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

        internal static AttributesTable ToAttributeTable(this TiledBarrierGraph.BarrierGraphEnumerator enumerator)
        {
            var attributes = enumerator.Tags.ToAttributeTable();

            attributes.Add("face_left", enumerator.FaceLeft);
            attributes.Add("face_right", enumerator.FaceRight);
            
            return attributes;
        }
    }
}