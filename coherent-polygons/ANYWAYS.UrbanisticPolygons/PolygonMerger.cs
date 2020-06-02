using System;
using System.Collections.Generic;
using System.Linq;
using OsmSharp.Complete;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons
{
    public interface IMergeFactorCalculator
    {
        double MergeImportance(UrbanPolygon a, UrbanPolygon b, IEnumerable<(long, long)> sharedEdges,
            Graph.Graph graph);
    }

    public class PolygonMerger
    {
        private readonly WayWeight<int> _barriers;
        private readonly Graph.Graph _graph;
        private readonly IMergeFactorCalculator _merger;

        private Dictionary<(long, long), HashSet<UrbanPolygon>> _edgeIndex;
        private double _avgWeight;


        public PolygonMerger(IEnumerable<(CompleteWay geometry, List<(long, long)> edges)> polygonEdges,
            WayWeight<int> barriers, Graph.Graph graph, IMergeFactorCalculator merger)
        {
            // So, we already made it this far. 
            // AT this point, we have polygon geometries encoded as graph edges (we don't care about the actual geometry at this point)
            // These edges are shared between two polygon (normally - there are literal edge cases here)
            // If an edge is shared, then the polygons can be merged

            var polygons = polygonEdges.Select(p => new UrbanPolygon(p.edges, p.geometry)).ToList();
            _avgWeight = polygons.Average(p => p.Area);
            _barriers = barriers;
            _graph = graph;
            _merger = merger;

            //  build an index of the polygons
            _edgeIndex = new Dictionary<(long, long), HashSet<UrbanPolygon>>();
            foreach (var p in polygons)
            {
                AddPolygon(p);
            }
        }

        private TagsCollectionBase MergeTags(TagsCollectionBase a, TagsCollectionBase b)
        {
            var classifications = new Dictionary<string, double>();

            foreach (var tag in a)
            {
                var value = double.Parse(tag.Value);
                classifications[tag.Key] = value;
            }

            foreach (var tag in b)
            {
                var value = double.Parse(tag.Value);
                classifications[tag.Key] =
                    value + classifications.GetValueOrDefault(tag.Key, 0.0);
            }

            var stringified = new Dictionary<string, string>();
            foreach (var classification in classifications)
            {
                stringified[classification.Key] = "" + classification.Value;
            }

            return new TagsCollection(stringified);
        }


        /// <summary>
        /// INspects the polygon and its neighbours.
        /// If a merge is needed, it'll perform the merge and update all the indexes.
        /// </summary>
        /// <param name="polygon">THe polygon to merge</param>
        /// <param name="mergeWith">The neighbouring polygon that this polygon is merged with</param>
        /// <returns>True if the merge happened</returns>
        private UrbanPolygon InspectAndMerge(UrbanPolygon polygon, out UrbanPolygon mergeWith)
        {
            var neighbours = Neighbours(polygon).ToList();

            var highestMergeFactor = double.MinValue;
            mergeWith = null;
            HashSet<(long, long)> mergeWithSharedEdges = null;
            foreach (var neighbour in neighbours)
            {
                var sharedEdges = neighbour.EdgeIds.Intersect(polygon.EdgeIds).ToHashSet();
                // We search for the polygon with the highest merge factor
                var mergeFactor = _merger.MergeImportance(polygon, neighbour, sharedEdges, _graph);
                if (highestMergeFactor < mergeFactor)
                {
                    highestMergeFactor = mergeFactor;
                    mergeWith = neighbour;
                    mergeWithSharedEdges = sharedEdges;
                }
            }


            if (mergeWith == null || mergeWithSharedEdges == null)
            {
                return null;
            }


            var newEdges = mergeWith.EdgeIds.Concat(polygon.EdgeIds).ToList();
            foreach (var shared in mergeWithSharedEdges)
            {
                // A shared edge will be in the list twice
                newEdges.Remove(shared);
                newEdges.Remove(shared);
            }


            var geometry = _graph.BuildOuterRing(newEdges);
            var summedArea = polygon.Area + mergeWith.Area;
            var newPolygon = new UrbanPolygon(
                newEdges,
                MergeTags(polygon.Tags, mergeWith.Tags),
                summedArea,
                geometry
            );


            return newPolygon;
        }

        public IEnumerable<CompleteWay> MergePolygons()
        {
            var mergedCount = 0;
            var mergedPolies = new HashSet<UrbanPolygon>();

            // We create a queue of edges sorted by their merge probability
            var priorityQueue = new List<((long, long) edgeId, double priority)>();


            foreach (var (edge, sharedBy) in _edgeIndex)
            {
                if (sharedBy.Count > 2)
                {
                    throw new Exception("Wut?");
                }

                if (sharedBy.Count <= 1)
                {
                    continue;
                }

                var a = sharedBy.First();
                var b = sharedBy.Last();

                var sharedEdges = a.EdgeIds.Intersect(b.EdgeIds);

                var mergeProb = _merger.MergeImportance(a, b, sharedEdges, _graph);
                priorityQueue.Add((edge, mergeProb));
            }

            // Note: we use the priority queue as a stack, so we but the highest values to the back
            priorityQueue = priorityQueue.OrderBy(e => e.priority).ToList();

            while (priorityQueue.Any())
            {
                var edgeToRemove = priorityQueue.Pop();

                var polyA = _edgeIndex[edgeToRemove.edgeId].First();
                var polyB = _edgeIndex[edgeToRemove.edgeId].Last();
                
                

            }
            
            
            return mergedPolies.Select(p => p.AsWay());
        }

        private void RemovePolygon(UrbanPolygon polygon)
        {
            // Remove from the indexes
            // There is _no_ need to remove this from the graph, as the graph only serves to keep track of geometry
            foreach (var edgeId in polygon.EdgeIds)
            {
                _edgeIndex[edgeId].Remove(polygon);
            }
        }

        private static void RemoveAndAdd(IList<UrbanPolygon> queueByArea, int start, UrbanPolygon toRemove,
            UrbanPolygon toAdd)
        {
            var i = start;
            for (; i < queueByArea.Count; i++)
            {
                if (queueByArea[i].Id == toRemove.Id)
                {
                    break;
                }
            }
            // At this point, i should be removed
            // We fill i with the next polygon (either toAdd or the next in the list)

            for (; i < queueByArea.Count; i++)
            {
                if (i + 1 == queueByArea.Count)
                {
                    // We have reached the end of the list, the toAdd polygon is the biggest
                    queueByArea[i] = toAdd;
                    return;
                }

                if (toAdd.Area <= queueByArea[i + 1].Area)
                {
                    queueByArea[i] = toAdd;
                    return;
                }

                queueByArea[i] = queueByArea[i + 1];
            }
        }

        private bool ContainsTileEdge(UrbanPolygon p)
        {
            return p.EdgeIds.Any(id =>
                _graph.GetGeometry(id).Tags.TryGetValue("_tile_edge", out var v) && v.Equals("yes"));
        }


        private HashSet<UrbanPolygon> Neighbours(UrbanPolygon p)
        {
            return p.EdgeIds.SelectMany(
                id => _edgeIndex[id].Where(polygon => polygon.Id != p.Id)).ToHashSet();
        }

        private void AddPolygon(UrbanPolygon p)
        {
            foreach (var edgeId in p.EdgeIds)
            {
                if (_edgeIndex.TryGetValue(edgeId, out var index))
                {
                    index.Add(p);
                }
                else
                {
                    _edgeIndex[edgeId] = new HashSet<UrbanPolygon> {p};
                }
            }
        }
    }
}