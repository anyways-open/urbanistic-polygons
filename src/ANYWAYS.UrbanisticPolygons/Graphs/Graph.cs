using System;
using System.Collections.Generic;

namespace ANYWAYS.UrbanisticPolygons.Graphs
{
    internal class Graph<TVertexData, TEdgeData, TFaceData>
    {
        private readonly List<(TVertexData vertex, int pointer)> _vertices = 
            new List<(TVertexData vertex, int pointer)>();
        private readonly List<(TEdgeData edge, int vertex1, int vertex2, int nextEdge1, int nextEdge2, int left, int right, int nextLeft, int nextRight, bool nextLeftLeft, bool nextRightLeft)> _edges = 
            new List<(TEdgeData edge, int vertex1, int vertex2, int nextEdge1, int nextEdge2, int left, int right, int nextLeft, int nextRight, bool nextLeftLeft, bool nextRightLeft)>();
        private readonly List<(TFaceData face, int edge, bool left)> _faces =
            new List<(TFaceData face, int edge, bool left)>();

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

        public int FaceCount => _faces.Count;

        public int AddEdge(int vertex1, int vertex2, TEdgeData edgeData)
        {
            var vertex1Meta = _vertices[vertex1];
            var vertex2Meta = _vertices[vertex2];
            
            var id = _edges.Count;
            _edges.Add((edgeData, vertex1, vertex2, vertex1Meta.pointer, vertex2Meta.pointer, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, false, false));

            _vertices[vertex1] = (vertex1Meta.vertex, id);
            _vertices[vertex2] = (vertex2Meta.vertex, id);
            
            return id;
        }

        public void ReverseEdge(int edge, Func<TEdgeData, TEdgeData>? reverse = null)
        {
            var edgeDetails = _edges[edge];
            var edgeData = edgeDetails.edge;
            if (reverse != null) edgeData = reverse(edgeData);
            
            _edges[edge] = (edgeData, edgeDetails.vertex2, edgeDetails.vertex1, edgeDetails.nextEdge2,
                edgeDetails.nextEdge1,
                edgeDetails.right, edgeDetails.left, edgeDetails.nextRight, edgeDetails.nextLeft,
                edgeDetails.nextRightLeft, edgeDetails.nextLeftLeft);
        }

        public void ResetFaces()
        {
            _faces.Clear();

            for (var e = 0; e < _edges.Count; e++)
            {
                var (edge, vertex1, vertex2, nextEdge1, nextEdge2, _, _, _, _, _, _) = _edges[e];
                _edges[e] = (edge, vertex1, vertex2, nextEdge1,
                    nextEdge2, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, false, false);
            }
        }

        public int AddFace(TFaceData face)
        {
            var id = _faces.Count;
            _faces.Add((face, int.MaxValue, false));
            return id;
        }

        public TFaceData GetFaceData(int face)
        {
            return _faces[face].face;
        }

        public void SetFaceData(int face, TFaceData data)
        {
            _faces[face] = (data, _faces[face].edge, _faces[face].left);
        }

        public void SetFace(int edge, bool left, int face)
        {
            var edgeDetails = _edges[edge];
            var faceDetails = _faces[face];
            if (left)
            {
                _edges[edge] = (edgeDetails.edge, edgeDetails.vertex1, edgeDetails.vertex2, edgeDetails.nextEdge1,
                    edgeDetails.nextEdge2,
                    face, edgeDetails.right, faceDetails.edge, edgeDetails.nextRight, faceDetails.left, edgeDetails.nextRightLeft);
            }
            else
            {
                _edges[edge] = (edgeDetails.edge, edgeDetails.vertex1, edgeDetails.vertex2, edgeDetails.nextEdge1,
                    edgeDetails.nextEdge2,
                    edgeDetails.left, face, edgeDetails.nextLeft, faceDetails.edge, edgeDetails.nextLeftLeft, faceDetails.left);
            }

            _faces[face] = (faceDetails.face, edge, left);
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
                    if (previousEdgeDetails.vertex1 == vertex && previousEdgeDetails.vertex2 == vertex)
                    {
                        _edges[previousPointer] = (previousEdgeDetails.edge, previousEdgeDetails.vertex1,
                            previousEdgeDetails.vertex2,
                            nextPointer, nextPointer, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, false, false);
                    }
                    else if (previousEdgeDetails.vertex1 == vertex)
                    {
                        _edges[previousPointer] = (previousEdgeDetails.edge, previousEdgeDetails.vertex1,
                            previousEdgeDetails.vertex2,
                            nextPointer, previousEdgeDetails.nextEdge2, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, false, false);
                    }
                    else
                    {
                        _edges[previousPointer] = (previousEdgeDetails.edge, previousEdgeDetails.vertex1,
                            previousEdgeDetails.vertex2,
                            previousEdgeDetails.nextEdge1, nextPointer, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, false, false);
                    }

                    return;
                }

                previousPointer = pointer;
                pointer = nextPointer;
            }
        }

        public Enumerator GetEdgeEnumerator()
        {
            return new Enumerator(this);
        }

        public class Enumerator
        {
            private readonly Graph<TVertexData, TEdgeData, TFaceData> _graph;

            public Enumerator(Graph<TVertexData, TEdgeData, TFaceData> graph)
            {
                _graph = graph;
            }

            private int _vertex = int.MaxValue;
            private int _nextEdge = int.MaxValue;
            private bool _forward = false;
            private (TEdgeData edge, int vertex1, int vertex2, int left, int right) _edge;
            
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
                    _edge = (edge.edge, edge.vertex1, edge.vertex2, edge.left, edge.right);
                }
                else
                {
                    _nextEdge = edge.nextEdge2;
                    _forward = false;
                    _edge = (edge.edge, edge.vertex2, edge.vertex1, edge.left, edge.right);
                }

                return true;
            }

            public bool Forward => _forward;

            public int Vertex1 => _edge.vertex1;

            public int Vertex2 => _edge.vertex2;

            public int FaceLeft => _edge.left;

            public int FaceRight => _edge.right;
            
            public int Edge { get; private set; }

            public TEdgeData Data => _edge.edge;
        }
        
        public FaceEnumerator GetFaceEnumerator()
        {
            return new FaceEnumerator(this);
        }

        public class FaceEnumerator
        {
            private readonly Graph<TVertexData, TEdgeData, TFaceData> _graph;

            public FaceEnumerator(Graph<TVertexData, TEdgeData, TFaceData> graph)
            {
                _graph = graph;
            }

            private int _face = int.MaxValue;
            private (int next, bool left) _nextEdge = (int.MaxValue, false);
            private (TEdgeData edge, int vertex1, int vertex2) _edge;
            
            public bool MoveTo(int face)
            {
                if (_graph._faces.Count <= face) return false;

                _face = face;
                var (_, edge, left) = _graph._faces[_face];
                _nextEdge = (edge, left);
                return true;
            }

            public bool MoveNext()
            {
                if (_nextEdge.next == int.MaxValue) return false;

                this.Edge = _nextEdge.next;
                var edge = _graph._edges[this.Edge];

                this.IsLeft = _nextEdge.left;
                if (_nextEdge.left)
                {
                    _nextEdge = (edge.nextLeft, edge.nextLeftLeft);
                }
                else
                {
                    _nextEdge = (edge.nextRight, edge.nextRightLeft);
                }
                
                _edge = (edge.edge, edge.vertex1, edge.vertex2);

                return true;
            }

            public bool IsLeft { get; private set; }

            public int Vertex1 => _edge.vertex1;

            public int Vertex2 => _edge.vertex2;
            
            public int Edge { get; private set; }

            public TEdgeData Data => _edge.edge;
        }
    }
}