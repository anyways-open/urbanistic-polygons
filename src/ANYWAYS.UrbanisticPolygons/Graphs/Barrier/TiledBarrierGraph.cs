using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ANYWAYS.UrbanisticPolygons.Geo;
using ANYWAYS.UrbanisticPolygons.Landuse;
using ANYWAYS.UrbanisticPolygons.Tiles;
using OsmSharp.Tags;

[assembly:InternalsVisibleTo("ANYWAYS.UrbanisticPolygons.Tests")]
namespace ANYWAYS.UrbanisticPolygons.Graphs.Barrier
{
    internal class TiledBarrierGraph
    {
        private readonly Graph<BarrierGraphVertex, BarrierGraphEdge, LanduseAttributes> _graph = 
            new Graph<BarrierGraphVertex, BarrierGraphEdge, LanduseAttributes>();
        private readonly Dictionary<long, int> _vertexNodes = new Dictionary<long, int>();
        private readonly HashSet<long> _ways = new HashSet<long>();
        private readonly HashSet<uint> _tiles = new HashSet<uint>();
        private readonly RTree<int> _vertexTree = new RTree<int>();

        public TiledBarrierGraph(int zoom = 14)
        {
            Zoom = zoom;
        }

        public int Zoom { get; }

        public int VertexCount => _graph.VertexCount;

        public int FaceCount => _graph.FaceCount;

        public bool TryGetVertex(long node, out int vertex)
        {
            return _vertexNodes.TryGetValue(node, out vertex);
        }

        public bool HasWay(long way)
        {
            return _ways.Contains(way);
        }

        public void SetTileLoaded(uint tile)
        {
            _tiles.Add(tile);
        }

        public IEnumerable<uint> LoadedTiles()
        {
            return _tiles;
        }

        public bool HasTile(uint tile)
        {
            return _tiles.Contains(tile);
        }

        public int AddVertex(double longitude, double latitude, long? node = null)
        {
            var vertexDetails = new BarrierGraphVertex()
            {
                Latitude = latitude,
                Longitude = longitude
            };
            var vertex = _graph.AddVertex(vertexDetails);
            if (node != null) _vertexNodes[node.Value] = vertex;
            return vertex;
        }

        public (double longitude, double latitude) GetVertex(int vertex)
        {
            var vertexDetails = _graph.GetVertex(vertex);
            return (vertexDetails.Longitude, vertexDetails.Latitude);
        }

        public ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)?
            GetVertexBox(int vertex)
        {
            return _graph.GetVertex(vertex).Box;
        }

        public int AddEdge(int vertex1, int vertex2, IEnumerable<(double longitude, double latitude)>? shape = null,
            TagsCollectionBase tags = null, long? way = null)
        {
            shape ??= Enumerable.Empty<(double longitude, double latitude)>();
            tags ??= new TagsCollection();
            
            if (way != null) _ways.Add(way.Value);
            
            var box = new [] { this.GetVertex(vertex1), this.GetVertex(vertex2)}.Concat(shape).ToBox();
            if (box == null) throw new Exception();

            var vertex1Details = _graph.GetVertex(vertex1);
            if (vertex1Details.Box.HasValue) _vertexTree.Remove(vertex1Details.Box.Value, vertex1);
            vertex1Details.Box = vertex1Details.Box.HasValue ?
                vertex1Details.Box = vertex1Details.Box.Value.Expand(box.Value) :
                box;
            _graph.SetVertex(vertex1, vertex1Details);
            _vertexTree.Add(vertex1Details.Box.Value, vertex1);
            
            var vertex2Details = _graph.GetVertex(vertex2);
            if (vertex2Details.Box.HasValue) _vertexTree.Remove(vertex2Details.Box.Value, vertex1);
            vertex2Details.Box = vertex2Details.Box.HasValue ?
                vertex2Details.Box = vertex2Details.Box.Value.Expand(box.Value) :
                box;
            _graph.SetVertex(vertex2, vertex2Details);
            _vertexTree.Add(vertex2Details.Box.Value, vertex2);
            
            return _graph.AddEdge(vertex1, vertex2, new BarrierGraphEdge()
            {
                Shape = shape.ToArray(),
                Tags = tags,
                Box = box.Value
            });
        }

        private ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)?
            BoxForVertex(int vertex)
        {
            var enumerator = this.GetEnumerator();
            enumerator.MoveTo(vertex);

            ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)? box = null;
            while (enumerator.MoveNext())
            {
                if (box == null)
                {
                    box = enumerator.Box;
                }
                else
                {
                    box = box.Value.Expand(enumerator.Box);
                }
            }

            return box;
        }

        public IEnumerable<int> GetVerticesOverlapping(((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box)
        {
            return _vertexTree.Get(box);
        }

        public void DeleteEdge(int edge)
        {
            var enumerator = this.GetEnumerator();
            enumerator.MoveToEdge(edge);
            var vertex1 = enumerator.Vertex1;
            var vertex2 = enumerator.Vertex2;

            _graph.DeleteEdge(edge);

            var vertex1Details = _graph.GetVertex(vertex1);
            if (vertex1Details.Box.HasValue) _vertexTree.Remove(vertex1Details.Box.Value, vertex1);
            vertex1Details.Box = BoxForVertex(vertex1);
            _graph.SetVertex(vertex1, vertex1Details);
            if (vertex1Details.Box.HasValue) _vertexTree.Add(vertex1Details.Box.Value, vertex1);

            var vertex2Details = _graph.GetVertex(vertex2);
            if (vertex2Details.Box.HasValue) _vertexTree.Remove(vertex2Details.Box.Value, vertex2);
            vertex2Details.Box = BoxForVertex(vertex2);
            _graph.SetVertex(vertex2, vertex2Details);
            if (vertex2Details.Box.HasValue) _vertexTree.Add(vertex2Details.Box.Value, vertex2);
        }
        
        public void ReverseEdge(int edge)
        {
            _graph.ReverseEdge(edge, ed => new BarrierGraphEdge()
            {
                Shape = ed.Shape.Reverse().ToArray(),
                Tags = ed.Tags,
                Box = ed.Box
            });
        }

        public void ResetFaces()
        {
            _graph.ResetFaces();
        }
        
        public int AddFace()
        {
            return _graph.AddFace(new LanduseAttributes());
        }

        public void SetFaceData(int face, LanduseAttributes data)
        {
            _graph.SetFaceData(face, data); 
        }

        public LanduseAttributes GetFaceData(int face)
        {
            return _graph.GetFaceData(face);
        }

        public void SetFace(int edge, bool left, int face)
        {
            _graph.SetFace(edge, left, face);
        }
        
        public BarrierGraphEnumerator GetEnumerator()
        {
            return new BarrierGraphEnumerator(this);
        }
        
        public BarrierGraphFaceEnumerator GetFaceEnumerator()
        {
            return new BarrierGraphFaceEnumerator(this);
        }

        private class BarrierGraphVertex
        {
            public double Latitude { get; set; }
            
            public double Longitude { get; set; }
            
            public ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)? Box
            {
                get;
                set;
            }
        }

        private class BarrierGraphEdge
        {
            public ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) Box
            {
                get;
                set;
            }
            
            public (double longitude, double latitude)[] Shape { get; set; }
        
            public TagsCollectionBase Tags { get; set; }
        }

        public class BarrierGraphEnumerator
        {
            private readonly Graph<BarrierGraphVertex, BarrierGraphEdge, LanduseAttributes>.Enumerator _enumerator;

            public BarrierGraphEnumerator(TiledBarrierGraph graph)
            {
                Graph = graph;
                
                _enumerator = graph._graph.GetEdgeEnumerator();
            }

            public bool MoveTo(int vertex)
            {
                return _enumerator.MoveTo(vertex);
            }

            public bool MoveToEdge(int edge)
            {
                return _enumerator.MoveToEdge(edge);
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public TiledBarrierGraph Graph { get; }

            public int Edge => _enumerator.Edge;

            public int Vertex1 => _enumerator.Vertex1;

            public int Vertex2 => _enumerator.Vertex2;

            public int FaceLeft => _enumerator.FaceLeft;

            public int FaceRight => _enumerator.FaceRight;

            public bool Forward => _enumerator.Forward;

            public (double longitude, double latitude)[] Shape => _enumerator.Data.Shape;

            public ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) Box =>
                _enumerator.Data.Box;

            public TagsCollectionBase Tags => _enumerator.Data.Tags;
        }
        
        public class BarrierGraphFaceEnumerator
        {
            private readonly Graph<BarrierGraphVertex, BarrierGraphEdge, LanduseAttributes>.FaceEnumerator _enumerator;

            public BarrierGraphFaceEnumerator(TiledBarrierGraph graph)
            {
                Graph = graph;
                
                _enumerator = graph._graph.GetFaceEnumerator();
            }

            public bool MoveTo(int face)
            {
                return _enumerator.MoveTo(face);
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public TiledBarrierGraph Graph { get; }

            public int Edge => _enumerator.Edge;

            public int Vertex1 => _enumerator.Vertex1;

            public int Vertex2 => _enumerator.Vertex2;

            public bool IsLeft => _enumerator.IsLeft;

            public (double longitude, double latitude)[] Shape => _enumerator.Data.Shape;

            public TagsCollectionBase Tags => _enumerator.Data.Tags;
        }
    }
}