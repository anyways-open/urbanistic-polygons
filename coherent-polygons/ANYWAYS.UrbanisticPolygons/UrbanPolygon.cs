using System.Collections.Generic;
using System.Linq;
using OsmSharp;
using OsmSharp.Complete;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons
{
    public class UrbanPolygon
    {
        private static int NextId = 0;

        /// <summary>
        /// Contains all edge-ids
        /// Note that a polygon might use a single edge twice, if it has to walk over it twice to reach an inner polygon
        /// </summary>
        public List<(long, long)> EdgeIds;

        public readonly Node[] Geometry;

        public readonly TagsCollectionBase Tags;
        public readonly double Area;
        public readonly int Id;

        public UrbanPolygon(List<(long, long)> edgeIds, TagsCollectionBase tags, double area, Node[] geometry)
        {
            Id = NextId;
            NextId++;
            EdgeIds = edgeIds;
            Tags = tags;
            Area = area;
            Geometry = geometry;
        }

        public UrbanPolygon(List<(long, long)> edgeIds, CompleteWay geometry)
        {
            Id = NextId;
            NextId++;
            EdgeIds = edgeIds;
            Geometry = geometry.Nodes;
            Tags = geometry.Tags ?? new TagsCollection();
            Area = geometry.Area();
        }

        public CompleteWay AsWay()
        {
            return new CompleteWay
            {
                Tags = new TagsCollection(Tags),
                Nodes = Geometry
            };
        }

        public string BiggestClassification()
        {
            var maxTag = "";
            var maxSurface = 0.0;
            foreach (var tag in Tags)
            {
                var surface = double.Parse(tag.Value);
                if (surface > maxSurface)
                {
                    maxTag = tag.Key;
                    maxSurface = surface;
                }
            }

            if (string.IsNullOrEmpty(maxTag))
            {
                return "No classification";
            }

            return maxTag.Substring("_classification:".Length);
        }

        public double GetRatio(string key)
        {
            if (Tags.TryGetValue(key, out var v))
            {
                var surface = double.Parse(v);
                return surface / Area;
            }

            return 0;
        }

        public double Perimeter(Graph.Graph graph)
        {
            return EdgeIds.Sum(id => graph.GetGeometry(id).Length());
        }
    }
}