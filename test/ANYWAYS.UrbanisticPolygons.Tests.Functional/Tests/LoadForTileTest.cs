using System;
using ANYWAYS.UrbanisticPolygons.Data;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons.Tests.Functional.Tests
{
    internal class LoadForTileTest : FunctionalTest<TiledBarrierGraph, (uint tile, OsmTileSource source, Func<TagsCollectionBase, bool> isBarrier)>
    {
        protected override TiledBarrierGraph Execute((uint tile, OsmTileSource source, Func<TagsCollectionBase, bool> isBarrier) input)
        {
            // load data for tile.
            var graph = new TiledBarrierGraph();
            graph.LoadForTile(input.tile, input.source.GetTile, input.isBarrier);

            return graph;
        }

        public static readonly LoadForTileTest Default = new LoadForTileTest();
    }
}