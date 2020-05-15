using System.Collections.Generic;
using System.Linq;
using OsmSharp.Complete;
using static ANYWAYS.UrbanisticPolygons.Graph.GraphUtils;

namespace ANYWAYS.UrbanisticPolygons.Graph
{
    public partial class Graph
    {
        public void AddWay(CompleteWay segment)
        {
            var start = segment.Nodes.First();
            var startId = start.NodeId();
            var end = segment.Nodes.Last();
            var endId = end.NodeId();

            if (startId == endId)
            {
                // This is a degenerate way, a loop, e.g. a (mini-)roundabout with a single entry
                // We don't add those
                return;
            }

            var edgeId = Id(startId, endId);
            _edges[edgeId] = segment;


            if (!_vertices.ContainsKey(startId))
            {
                _vertices[startId] = (new HashSet<long>(), start);
            }

            if (!_vertices.ContainsKey(endId))
            {
                _vertices[endId] = (new HashSet<long>(), end);
            }

            _vertices[startId].connections.Add(endId);
            _vertices[endId].connections.Add(startId);
        }
    }
}