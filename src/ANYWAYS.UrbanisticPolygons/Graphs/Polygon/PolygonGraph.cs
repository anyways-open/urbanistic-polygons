using System.Collections.Generic;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons.Graphs.Polygon
{
    internal class PolygonGraph
    {
        private readonly TiledBarrierGraph _graph = new TiledBarrierGraph();
        private readonly (int left, int right)[] _edgeFaces;
        private readonly List<Face> _faces;

        private struct Face
        {
            public int FirstEdge { get; set; }
        }

        private struct Edge
        {
            public TagsCollectionBase BarrierTags { get; set; }
        }
    }
}