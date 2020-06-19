using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using OsmSharp.Tags;

[assembly:InternalsVisibleTo("ANYWAYS.UrbanisticPolygons.Tests")]
namespace ANYWAYS.UrbanisticPolygons.Graphs.Barrier
{
    internal class TiledBarrierGraph
    {
        private readonly Graph<(double lon, double lat), BarrierGraphEdge> _graph = new Graph<(double lon, double lat), BarrierGraphEdge>();
        private readonly Dictionary<long, int> _vertexNodes = new Dictionary<long, int>();
        private readonly HashSet<long> _ways = new HashSet<long>();
        private readonly HashSet<uint> _tiles = new HashSet<uint>();

        public TiledBarrierGraph(int zoom = 14)
        {
            Zoom = zoom;
        }

        public int Zoom { get; }

        public int VertexCount => _graph.VertexCount;

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
            
            return _graph.AddEdge(vertex1, vertex2, new BarrierGraphEdge()
            {
                Shape = shape.ToArray(),
                Tags = tags
            });
        }

        public void DeleteEdge(int edge)
        {
            _graph.DeleteEdge(edge);
        }
        
        public BarrierGraphEnumerator GetEnumerator()
        {
            return new BarrierGraphEnumerator(this);
        }

        private struct BarrierGraphEdge
        {
            public (double longitude, double latitude)[] Shape { get; set; }
        
            public TagsCollectionBase Tags { get; set; }
        }

        public class BarrierGraphEnumerator
        {
            private readonly Graph<(double lon, double lat), BarrierGraphEdge>.Enumerator _enumerator;

            public BarrierGraphEnumerator(TiledBarrierGraph graph)
            {
                Graph = graph;
                
                _enumerator = graph._graph.GetEnumerator();
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

            public bool Forward => _enumerator.Forward;

            public (double longitude, double latitude)[] Shape => _enumerator.Data.Shape;

            public TagsCollectionBase Tags => _enumerator.Data.Tags;
        }
    }
}