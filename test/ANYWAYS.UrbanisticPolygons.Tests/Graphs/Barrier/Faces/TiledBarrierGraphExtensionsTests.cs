using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Faces;
using Xunit;

namespace ANYWAYS.UrbanisticPolygons.Tests.Graphs.Barrier.Faces
{
    public class TiledBarrierGraphExtensionsTests
    {
        [Fact]
        public void TiledBarrierGraphExtensions_NextClockwise_NoOtherEdge_ShouldBeEmpty()
        {
            var graphs = new TiledBarrierGraph();
            var v1 = graphs.AddVertex(4.7522735595703125, 50.97918242660188, 564341430);
            var v2 = graphs.AddVertex(4.7525310516357420, 50.97851368626033, 564341431);
            var e = graphs.AddEdge(v1, v2);

            var enumerator = graphs.GetEnumerator();
            enumerator.MoveTo(v1);
            enumerator.MoveNext();

            var clockwise = enumerator.NextClockwise();
            Assert.Empty(clockwise);
        }
        
        [Fact]
        public void TiledBarrierGraphExtensions_NextClockwise_OneOtherEdge_ShouldBeSingle()
        {
            var graphs = new TiledBarrierGraph();
            var v1 = graphs.AddVertex(
                4.801331162452698,
                51.267829233580834);
            var v2 = graphs.AddVertex(
                4.801325798034667,
                51.268153126307524);
            var v3 = graphs.AddVertex(
                4.801816642284393,
                51.26783426820025);
            var e1 = graphs.AddEdge(v2, v1);
            var e2 = graphs.AddEdge(v1, v3);
            
            var enumerator = graphs.GetEnumerator();
            enumerator.MoveTo(v2);
            enumerator.MoveNext();
        
            var clockwise = enumerator.NextClockwise();
            Assert.Single(clockwise);
        }

        [Fact]
        public void TiledBarrierGraphExtensions_NextClockwise_TwoOtherEdges_ShouldBeClockWise()
        {
            var graphs = new TiledBarrierGraph();
            var v1 = graphs.AddVertex(
                4.801325798034667,
                51.268153126307524);
            var v2 = graphs.AddVertex(
                4.801331162452698,
                51.267829233580834);
            var v3 = graphs.AddVertex(
                4.801816642284393,
                51.26783426820025);
            var v4 = graphs.AddVertex(
                4.801317751407623,
                51.26754225936128);
            var e1 = graphs.AddEdge(v1, v2);
            var e2 = graphs.AddEdge(v2, v3);
            var e3 = graphs.AddEdge(v2, v4);
            
            var enumerator = graphs.GetEnumerator();
            enumerator.MoveTo(v1);
            enumerator.MoveNext();
        
            using var clockwise = enumerator.NextClockwise().GetEnumerator();
            Assert.True(clockwise.MoveNext());
            Assert.Equal(v4, clockwise.Current.Vertex2);
            Assert.True(clockwise.MoveNext());
            Assert.Equal(v3, clockwise.Current.Vertex2);
            Assert.False(clockwise.MoveNext());
        }
    }
}