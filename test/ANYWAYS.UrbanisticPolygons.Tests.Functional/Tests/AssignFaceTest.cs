using System.Collections.Generic;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Faces;

namespace ANYWAYS.UrbanisticPolygons.Tests.Functional.Tests
{
    internal class AssignFaceTest : FunctionalTest<(bool success, IEnumerable<uint> missingTiles), (TiledBarrierGraph graph, uint tile)>
    {
        protected override (bool success, IEnumerable<uint> missingTiles) Execute((TiledBarrierGraph graph, uint tile) input)
        {
            return input.graph.AssignFaces(input.tile);
        }

        public static readonly AssignFaceTest Default = new AssignFaceTest();
    }
}