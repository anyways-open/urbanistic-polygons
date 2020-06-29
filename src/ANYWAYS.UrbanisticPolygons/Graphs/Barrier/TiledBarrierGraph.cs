using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ANYWAYS.UrbanisticPolygons.Landuse;
using ANYWAYS.UrbanisticPolygons.Tiles;
using OsmSharp.Tags;

[assembly:InternalsVisibleTo("ANYWAYS.UrbanisticPolygons.Tests")]
namespace ANYWAYS.UrbanisticPolygons.Graphs.Barrier
{
    internal class TiledBarrierGraph
    {
        private readonly Graph<(double lon, double lat), BarrierGraphEdge, LanduseAttributes> _graph = new Graph<(double lon, double lat), BarrierGraphEdge, LanduseAttributes>();
        private readonly Dictionary<long, int> _vertexNodes = new Dictionary<long, int>();
        private readonly HashSet<long> _ways = new HashSet<long>();
        private readonly HashSet<uint> _tiles = new HashSet<uint>();

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
            var vertex = _graph.AddVertex((longitude, latitude));
            if (node != null) _vertexNodes[node.Value] = vertex;
            return vertex;
        }

        public (double longitude, double latitude) GetVertex(int vertex)
        {
            return _graph.GetVertex(vertex);
        }

        public int AddEdge(int vertex1, int vertex2, IEnumerable<(double longitude, double latitude)>? shape = null,
            TagsCollectionBase tags = null, long? way = null)
        {
            shape ??= Enumerable.Empty<(double longitude, double latitude)>();
            tags ??= new TagsCollection();
            
            if (way != null) _ways.Add(way.Value);
            
            var box = new [] { this.GetVertex(vertex1), this.GetVertex(vertex2)}.Concat(shape).ToBox();
            if (box == null) throw new Exception();
            
            return _graph.AddEdge(vertex1, vertex2, new BarrierGraphEdge()
            {
                Shape = shape.ToArray(),
                Tags = tags,
                Box = box.Value
            });
        }

        public void DeleteEdge(int edge)
        {
            _graph.DeleteEdge(edge);
        }
        
        public void ReverseEdge(int edge)
        {
            _graph.ReverseEdge(edge, ed => new BarrierGraphEdge()
            {
                Shape = ed.Shape.Reverse().ToArray(),
                Tags = ed.Tags
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

        private struct BarrierGraphEdge
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
            private readonly Graph<(double lon, double lat), BarrierGraphEdge, LanduseAttributes>.Enumerator _enumerator;

            public BarrierGraphEnumerator(TiledBarrierGraph graph)
            {
                Graph = graph;
                
                _enumerator = graph._graph.GetEdgeEnumerator();
            }

            public bool MoveTo(int vertex)
            {
                return _enumerator.MoveTo(vertex);
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
            private readonly Graph<(double lon, double lat), BarrierGraphEdge, LanduseAttributes>.FaceEnumerator _enumerator;

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