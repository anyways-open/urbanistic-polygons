using System.Collections.Generic;

namespace ANYWAYS.UrbanisticPolygons.Graphs
{
    internal class Graph<TVertexData, TEdgeData>
    {
        private readonly List<(TVertexData vertex, int pointer)> _vertices = 
            new List<(TVertexData vertex, int pointer)>();
        private readonly List<(TEdgeData edge, int vertex1, int vertex2, int nextEdge1, int nextEdge2)> _edges = 
            new List<(TEdgeData edge, int vertex1, int vertex2, int nextEdge1, int nextEdge2)>();

        public int AddVertex(TVertexData vertex)
        {
            var id = _vertices.Count;
            _vertices.Add((vertex, int.MaxValue));
            return id;
        }

        public TVertexData GetVertex(int vertex)
        {
            return _vertices[vertex].vertex;
        }

        public int VertexCount => _vertices.Count;

        public int AddEdge(int vertex1, int vertex2, TEdgeData edgeData)
        {
            var vertex1Meta = _vertices[vertex1];
            var vertex2Meta = _vertices[vertex2];
            
            var id = _edges.Count;
            _edges.Add((edgeData, vertex1, vertex2, vertex1Meta.pointer, vertex2Meta.pointer));

            _vertices[vertex1] = (vertex1Meta.vertex, id);
            _vertices[vertex2] = (vertex2Meta.vertex, id);
            
            return id;
        }

        public void DeleteEdge(int edge)
        {
            var edgeDetails = _edges[edge];

            RemoveEdgeForVertex(edgeDetails.vertex1, edge);
            RemoveEdgeForVertex(edgeDetails.vertex2, edge);
        }

        private void RemoveEdgeForVertex(int vertex, int edge)
        {
            var vertexDetails = _vertices[vertex];
            var pointer = vertexDetails.pointer;
            var previousPointer = int.MaxValue;
            while (pointer != int.MaxValue)
            {
                var pointerEdgeDetails = _edges[pointer];
                
                // get next pointer.
                var nextPointer = pointerEdgeDetails.nextEdge1;
                if (pointerEdgeDetails.vertex2 == vertex)
                {
                    nextPointer = pointerEdgeDetails.nextEdge2;
                }

                if (pointer == edge)
                {
                    // the edge is found, remove it now!
                    if (previousPointer == int.MaxValue)
                    {
                        // edge was the first edge, overwrite the pointer with the next pointer.
                        _vertices[vertex] = (vertexDetails.vertex, nextPointer);
                        return;
                    }

                    // edge is not first edge, overwrite pointer on the previous edge.
                    var previousEdgeDetails = _edges[previousPointer];
                    if (previousEdgeDetails.vertex1 == vertex)
                    {
                        _edges[previousPointer] = (previousEdgeDetails.edge, previousEdgeDetails.vertex1,
                            previousEdgeDetails.vertex2,
                            nextPointer, previousEdgeDetails.nextEdge2);
                    }
                    else
                    {
                        _edges[previousPointer] = (previousEdgeDetails.edge, previousEdgeDetails.vertex1,
                            previousEdgeDetails.vertex2,
                            previousEdgeDetails.nextEdge1, nextPointer);
                    }

                    return;
                }

                previousPointer = pointer;
                pointer = nextPointer;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public class Enumerator
        {
            private readonly Graph<TVertexData, TEdgeData> _graph;

            public Enumerator(Graph<TVertexData, TEdgeData> graph)
            {
                _graph = graph;
            }

            private int _vertex = int.MaxValue;
            private int _nextEdge = int.MaxValue;
            private bool _forward = false;
            private (TEdgeData edge, int vertex1, int vertex2) _edge;

            public void Reset()
            {
                _vertex = int.MaxValue;
                _nextEdge = int.MaxValue;
                _forward = false;
            }
            
            public bool MoveTo(int vertex)
            {
                if (_graph._vertices.Count <= vertex) return false;

                _vertex = vertex;
                _nextEdge = _graph._vertices[_vertex].pointer;
                return true;
            }

            public bool MoveNext()
            {
                if (_nextEdge == int.MaxValue) return false;

                this.Edge = _nextEdge;
                var edge = _graph._edges[_nextEdge];

                if (edge.vertex1 == _vertex)
                {
                    _nextEdge = edge.nextEdge1;
                    _forward = true;
                    _edge = (edge.edge, edge.vertex1, edge.vertex2);
                }
                else
                {
                    _nextEdge = edge.nextEdge2;
                    _forward = false;
                    _edge = (edge.edge, edge.vertex2, edge.vertex1);
                }

                return true;
            }

            public bool Forward => _forward;

            public int Vertex1 => _edge.vertex1;

            public int Vertex2 => _edge.vertex2;
            
            public int Edge { get; private set; }

            public TEdgeData Data => _edge.edge;
        }
    }
}