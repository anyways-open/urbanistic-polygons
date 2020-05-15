using System;
using System.Collections.Generic;
using System.Linq;
using OsmSharp;
using OsmSharp.Complete;
using static ANYWAYS.UrbanisticPolygons.Graph.GraphUtils;

namespace ANYWAYS.UrbanisticPolygons.Graph
{
    public partial class Graph
    {
        private readonly Dictionary<(long smallestId, long largestId), CompleteWay> _edges =
            new Dictionary<(long smallestId, long largestId), CompleteWay>();

        private readonly Dictionary<long, (HashSet<long> connections, Node n)> _vertices =
            new Dictionary<long, (HashSet<long>, Node n)>();

        /// <summary>
        /// All vertexes with only two edges are removed, those two edges are taken together
        /// </summary>
        public void FuseEdges()
        {
            var toRemove = new HashSet<long>();
            foreach (var (vertexId, (connections, _)) in _vertices)
            {
                if (connections.Count != 2)
                {
                    continue;
                }

                var cons = connections.ToList();

                var a = cons[0];
                var b = cons[1];

                var c0 = Id(vertexId, a);
                var c1 = Id(vertexId, b);

                var edge0 = _edges[c0];
                var edge1 = _edges[c1];

                _edges.Remove(c0);
                _edges.Remove(c1);

                Node[] geometry;
                if (edge0.Nodes.Last().NodeId() == edge1.Nodes[0].NodeId())
                {
                    geometry = edge0.Nodes.Concat(edge1.Nodes).ToArray();
                }else if (edge1.Nodes.Last().NodeId() == edge0.Nodes[0].NodeId())
                {
                    geometry = edge1.Nodes.Concat(edge0.Nodes).ToArray();
                }else if (edge0.Nodes[0].NodeId() == edge1.Nodes[0].NodeId())
                {
                    geometry = edge0.Nodes.Reverse().Concat(edge1.Nodes).ToArray();
                }
                else
                {
                    // edge0.Last == edge1.Last
                    geometry = edge0.Nodes.Concat(edge1.Nodes.Reverse()).ToArray();
                }
                
                var fused = new CompleteWay()
                {
                    Nodes = geometry,
                    Tags = edge0.Tags
                };
                
                _edges[Id(a, b)] = fused;
                toRemove.Add(vertexId);

                _vertices[a].connections.Remove(vertexId);
                _vertices[a].connections.Add(b);

                _vertices[b].connections.Remove(vertexId);
                _vertices[b].connections.Add(a);
            }

            foreach (var id in toRemove)
            {
                _vertices.Remove(id);
            }
        }

        public void PruneDeadEnds()
        {
            var idsToRemove = new HashSet<long>();
            var edgesToRemove = new HashSet<(long, long)>();

            bool prunedSomething;
            do
            {
                prunedSomething = false;
                foreach (var (id, (connections, _)) in _vertices)
                {
                    if (connections.Count > 1)
                    {
                        continue;
                    }

                    if (idsToRemove.Contains(id))
                    {
                        // already removed
                        continue;
                    }

                    prunedSomething = true;
                    idsToRemove.Add(id);
                    foreach (var connection in connections)
                    {
                        edgesToRemove.Add(Id(id, connection));
                        _vertices[connection].connections.Remove(id);
                    }
                }
            } while (prunedSomething);

            foreach (var idToRemove in idsToRemove)
            {
                _vertices.Remove(idToRemove);
            }

            foreach (var key in edgesToRemove)
            {
                _edges.Remove(key);
            }
        }

        public List<CompleteWay> GetPolygons()
        {
            /* Determines all the polygons
             We know that every edge should be part of exactly two polygons;
             so this gives us a starting point: enumerate every edge, start walking along one side
             
             Note that the graph is planar, so there are no crossings
             */

            var forbiddenOrders = new HashSet<((long, long), (long, long))>();

            var polygons = new List<CompleteWay>();
            foreach (var ((from, to), geometry) in _edges)
            {
                var poly = WalkAround(from, to, forbiddenOrders);
                if (poly == null)
                {
                    continue;
                }

                polygons.Add(poly);
            }

            return polygons;
        }


        /// <summary>
        /// For the given vertex, gives a dictionary where the keys are all the connected other vertices.
        /// The values is the departure angle of this way (respective to the north
        /// </summary>
        /// <returns></returns>
        private Dictionary<long, double> DetermineAngles(long vertex)
        {
            var connectedVertices = _vertices[vertex].connections;

            var angles = new Dictionary<long, double>();

            foreach (var connectedVertex in connectedVertices)
            {
                var way = _edges[Id(connectedVertex, vertex)];
                var vertexPoint = way.Nodes[0];
                var nextPoint = way.Nodes[1];

                if (vertexPoint.NodeId() != vertex)
                {
                    vertexPoint = way.Nodes[way.Nodes.Length - 1];
                    nextPoint = way.Nodes[way.Nodes.Length - 2];
                }

                var angle = Utils.GetDegreesAzimuth(vertexPoint, nextPoint);
                angles[connectedVertex] = angle;
            }


            return angles;
        }


        public string AsGeoJson()
        {
            var all = new List<(ICompleteOsmGeo, Dictionary<string, object>)>();

            foreach (var (id, (connections, n )) in _vertices)
            {
                var attr = new Dictionary<string, object>
                {
                    {"debug-id", id}
                };

                foreach (var (otherId, angle) in DetermineAngles(id))
                {
                    attr.Add("other-id-" + otherId, angle);
                }

                all.Add((n, attr));
            }

            foreach (var (ids, edge) in _edges)
            {
                all.Add((edge, new Dictionary<string, object>
                {
                    {"edge-id", ids}
                }));
            }


            return all.AsGeoJson();
        }
    }
}