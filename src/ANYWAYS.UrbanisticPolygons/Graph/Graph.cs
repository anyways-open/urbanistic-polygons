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
        private readonly Tile _bbox;

        private readonly Dictionary<(long smallestId, long largestId), CompleteWay> _edges =
            new Dictionary<(long smallestId, long largestId), CompleteWay>();

        private readonly Dictionary<long, (HashSet<long> connections, Node n)> _vertices =
            new Dictionary<long, (HashSet<long>, Node n)>();

        public Graph(Tile bbox)
        {
            _bbox = bbox;
        }

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

                _edges.Remove(c0);
                _edges.Remove(c1);

                var fused = new CompleteWay()
                {
                    Nodes = Utils.FuseGeometry(_edges[c0].Nodes, _edges[c1].Nodes),
                    Tags = _edges[c0].Tags
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
        
        public Node[] BuildOuterRing(List<(long, long)> ids)
        {
            var (geometry, notMatched) = BuildPolygonFrom(ids);
            if (notMatched == null)
            {
                return geometry;
            }

            var way = new CompleteWay()
            {
                Nodes = geometry
            };
            var fullyContained = true;
            foreach (var noMatch in notMatched)
            {
               fullyContained &= way.FullyContains(_edges[noMatch]);
            }

            if (fullyContained)
            {
                return geometry;
            }
            
            // The geometry we have, is the inner ring - we simply discard it
            return BuildOuterRing(notMatched);

        }

        public (Node[] geometry, List<(long, long)> notMatched) BuildPolygonFrom(List<(long, long)> ids)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            Node[] nodes = null;
            // We skip all entries with exactly two occurences.
            // WHen a smaller polygon is contained within a bigger polygon, the bigger polygon will have a connection from the outer ring to the inner ring
            // This connection will have every edge listed twice - it is exactly this connection that is removed here

            var notDouble = new HashSet<(long, long)>();
            foreach (var id in ids)
            {
                if (notDouble.Contains(id))
                {
                    notDouble.Remove(id);
                }
                else
                {
                    notDouble.Add(id);
                }
                
            }
            
            var leftOvers = notDouble.ToList();
            while (leftOvers.Any())
            {
                var notMatched = new List<(long, long)>();
                foreach (var id in leftOvers)
                {
                    var geo = _edges[id].Nodes;
                    if (nodes == null)
                    {
                        nodes = geo;
                        continue;
                    }

                    var fused = Utils.FuseGeometry(geo, nodes);
                    if (fused == null)
                    {
                        notMatched.Add(id);
                        continue;
                    }

                    nodes = fused;
                }

                if (notMatched.Count == leftOvers.Count())
                {
                    
                    // If we get stuck here, we can't walk any further. This is probably because there is an inner polygon
                    // We just give back what was not matched + the geometry
                    return (nodes, notMatched);

                }

                leftOvers = notMatched;
            }

            return (nodes, null);
        }

        public void RemoveVertex(long vertexid)
        {
            var (connections, _) = _vertices[vertexid];
            _vertices.Remove(vertexid);
            foreach (var connection in connections)
            {
                _vertices[connection].connections.Remove(vertexid);
                _edges.Remove(Id(connection, vertexid));
            }
        }

        public Graph PruneDeadEnds()
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

            return this;
        }

        // public List<(CompleteWay geometry, List<(long, long)> boundaryEdges)> GetPolygons()
        // {
        //     /* Determines all the polygons
        //      We know that every edge should be part of exactly two polygons;
        //      so this gives us a starting point: enumerate every edge, start walking along one side
        //      
        //      Note that the graph is planar, so there are no crossings
        //      */
        //
        //     var forbiddenOrders = new HashSet<((long, long), (long, long))>();
        //
        //     var polygons = new List<(CompleteWay geometry, List<(long, long)> boundaryEdges)>();
        //     foreach (var ((from, to), _) in _edges)
        //     {
        //         var poly = WalkAround(from, to, forbiddenOrders);
        //         if (poly == null)
        //         {
        //             continue;
        //         }
        //
        //         if (poly.Value.geometry.IsClockwise())
        //         {
        //             // THis is the outline polygon containing everything
        //             // We skip it
        //             continue;
        //         }
        //
        //         polygons.Add(poly.Value);
        //     }
        //
        //     return polygons;
        // }


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

        public CompleteWay GetGeometry((long, long) id)
        {
            return _edges[Id(id)];
        }


        public string AsGeoJson()
        {
            var all = new List<(ICompleteOsmGeo, Dictionary<string, object>)>();

            foreach (var (id, (_, n )) in _vertices)
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