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
        private readonly Graph.Graph _graph;
        private readonly IMergeFactorCalculator _merger;
        private readonly uint _atMostNumberOfPolygons;

        private Dictionary<(long, long), HashSet<UrbanPolygon>> _edgeIndex;


        public PolygonMerger(IEnumerable<(CompleteWay geometry, List<(long, long)> edges)> polygonEdges,
            Graph.Graph graph, IMergeFactorCalculator merger, uint atMostNumberOfPolygons)
        {
            // So, we already made it this far. 
            // AT this point, we have polygon geometries encoded as graph edges (we don't care about the actual geometry at this point)
            // These edges are shared between two polygon (normally - there are literal edge cases here)
            // If an edge is shared, then the polygons can be merged

            var polygons = polygonEdges.Select(p => new UrbanPolygon(p.edges, p.geometry)).ToList();
            _graph = graph;
            _merger = merger;
            _atMostNumberOfPolygons = atMostNumberOfPolygons;

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
        private UrbanPolygon Merge(UrbanPolygon polygon, UrbanPolygon mergeWith)
        {
            var mergeWithSharedEdges = mergeWith.EdgeIds.Intersect(polygon.EdgeIds).ToHashSet();
            var newEdges = mergeWith.EdgeIds.Concat(polygon.EdgeIds).ToList();
            foreach (var shared in mergeWithSharedEdges)
            {
                // A shared edge will be in the list twice
                newEdges.Remove(shared);
                newEdges.Remove(shared);
            }

            var geometry = _graph.BuildOuterRing(newEdges);
            geometry[0] = geometry.Last();
            var summedArea = polygon.Area + mergeWith.Area;
            var newPolygon = new UrbanPolygon(
                newEdges,
                MergeTags(polygon.Tags, mergeWith.Tags),
                summedArea,
                geometry
            );

            return newPolygon;
        }

        private bool HasBoundaryEdge(UrbanPolygon polygon)
        {
            return polygon.EdgeIds
                .Any(id =>
                {
                    var tags = _graph.GetGeometry(id).Tags;
                    if (tags.TryGetValue("_tile_edge", out var v))
                    {
                        return v == "yes";
                    }

                    return false;
                });
        }

        public IEnumerable<CompleteWay> MergePolygons()
        {
            var allPolygons = _edgeIndex.SelectMany(kv => kv.Value).ToHashSet();


            var priorityQueue = BuildQueue();

            var hasEdge = new HashSet<UrbanPolygon>();

            foreach (var polygon in allPolygons)
            {
                if (HasBoundaryEdge(polygon))
                {
                    hasEdge.Add(polygon);
                }
            }

            while (priorityQueue.Any() && allPolygons.Count - hasEdge.Count > _atMostNumberOfPolygons)
            {
                var smallest = allPolygons.Except(hasEdge).Min(p => p.Area);
                var edgeToRemove = priorityQueue.Pop();

                var polies = _edgeIndex[edgeToRemove.edgeId];
                if (polies.Count == 1)
                {
                    continue;
                }

                var polyA = polies.First();
                var polyB = polies.Last();

                var newPoly = Merge(polyA, polyB);

                allPolygons.Remove(polyA);
                allPolygons.Remove(polyB);
                allPolygons.Add(newPoly);

                hasEdge.Remove(polyA);
                hasEdge.Remove(polyB);
                if (HasBoundaryEdge(newPoly))
                {
                    hasEdge.Add(newPoly);
                }

                RemovePolygon(polyA);
                RemovePolygon(polyB);
                AddPolygon(newPoly);


                priorityQueue = UpdateQueue(priorityQueue, polyA, polyB, newPoly);
            }

            return allPolygons.Select(p => p.AsWay());
        }

        private List<((long, long) edgeId, double priority)> UpdateQueue(
            List<((long, long) edgeId, double priority)> priorityQueue, UrbanPolygon polyA, UrbanPolygon polyB,
            UrbanPolygon newPoly)
        {
            // All the edges have to be recalculated
            // TODO This is not really performant
            var newQueue = new List<((long, long) edgeId, double priority)>();

            foreach (var (edgeId, priority) in priorityQueue)
            {
                if (polyA.EdgeIds.Contains(edgeId) || polyB.EdgeIds.Contains(edgeId))
                {
                    continue;
                }

                newQueue.Add((edgeId, priority));
            }

            foreach (var edgeId in newPoly.EdgeIds)
            {
                var sharedBy = _edgeIndex[edgeId];
                var a = sharedBy.First();
                var b = sharedBy.Last();

                var sharedEdges = a.EdgeIds.Intersect(b.EdgeIds);

                var mergeProb = _merger.MergeImportance(a, b, sharedEdges, _graph);
                newQueue.Add((edgeId, mergeProb));
            }

            return newQueue.OrderBy(e => e.priority).ToList();
        }

        /// <summary>
        ///
        /// Builds the initial priority queue
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private List<((long, long) edgeId, double priority)> BuildQueue()
        {
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
            return priorityQueue;
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