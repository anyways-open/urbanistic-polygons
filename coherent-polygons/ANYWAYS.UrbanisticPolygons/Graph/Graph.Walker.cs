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
        public CompleteWay WalkAround(long @from, long to, HashSet<((long, long), (long, long))> forbiddenOrders)
        {
            var firstId = Id(from, to);
            var currentId = firstId;

            var geometry = new List<Node>();

            void AddLine((long, long) id)
            {
                if (geometry.Count == 0)
                {
                    geometry.AddRange(_edges[currentId].Nodes);
                    return;
                }

                var toAdd = _edges[currentId].Nodes;

                if (toAdd[0].NodeId() == geometry[0].NodeId()
                    || toAdd.Last().NodeId() == geometry[0].NodeId())
                {
                    // The first point of the geometry matches this line, we simply reverse everything
                    geometry.Reverse();
                }

                if (toAdd[0].NodeId() == geometry.Last().NodeId())
                {
                    // The order is correct, we can simply append to the end
                    // We skip the first element though, as we are not supposed to copy it
                    geometry.AddRange(toAdd.SubArray(1));
                    return;
                }

                if (toAdd.Last().NodeId() == geometry.Last().NodeId())
                {
                    // ToAdd has a reversed order; the last element of toAdd doesn't have to be added again
                    for (int i = toAdd.Length - 2; i >= 0; i--)
                    {
                        geometry.Add(toAdd[i]);
                    }

                    return;
                }
            }

            AddLine(currentId);


            do
            {
                long nextDestination = 0;
                var angles = DetermineAngles(to);

                var sortedAngles = angles.OrderBy(kv => kv.Value).ToList();

                var pickNext = false;
                foreach (var (key, value) in sortedAngles)
                {
                    if (pickNext)
                    {
                        nextDestination = key;
                        pickNext = false;
                        break;
                    }

                    if (key == from)
                    {
                        pickNext = true;
                    }
                }

                if (pickNext)
                {
                    // The incoming edge was the last in the angle list
                    nextDestination = sortedAngles[0].Key;
                }


                if (from == to)
                {
                    var p = new CompleteWay()
                    {
                        Nodes = geometry.ToArray()
                    };
                    throw new ArgumentException("uh oh: " + p.AsGeoJson());
                }

                from = to;
                to = nextDestination;

                var nextId = Id(from, to);

                if (forbiddenOrders.Contains((currentId, nextId)))
                {
                    return null;
                }

                forbiddenOrders.Add((currentId, nextId));

                currentId = nextId;
                AddLine(currentId);
            } while (currentId != firstId);


            var polygon = new CompleteWay()
            {
                Nodes = geometry.ToArray()
            };
            if (geometry[0].NodeId() != geometry.Last().NodeId())
            {
                Console.WriteLine("Skipped polygon: not closed:\n" + polygon.AsGeoJson());
                return null;
            }

            polygon.Nodes[0] = polygon.Nodes.Last(); // SOmetimes, the start- and endpoint are different in the 10th digit after the dot, so we make sure they are the same

            return polygon;
        }
    }
}