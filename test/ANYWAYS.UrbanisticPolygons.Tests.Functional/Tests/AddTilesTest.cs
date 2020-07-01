using System;
using System.Collections.Generic;
using ANYWAYS.UrbanisticPolygons.Data;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons.Tests.Functional.Tests
{
    internal class AddTilesTest : FunctionalTest<object?, (TiledBarrierGraph graph, IEnumerable<uint> missingTiles, OsmTileSource source, Func<TagsCollectionBase, bool> isBarrier)>
    {
        protected override object? Execute((TiledBarrierGraph graph, IEnumerable<uint> missingTiles, OsmTileSource source, Func<TagsCollectionBase, bool> isBarrier) input)
        {
            input.graph.AddTiles(input.missingTiles, input.source.GetTile, input.isBarrier);

            return null;
        }

        public static readonly AddTilesTest Default = new AddTilesTest();
    }
}