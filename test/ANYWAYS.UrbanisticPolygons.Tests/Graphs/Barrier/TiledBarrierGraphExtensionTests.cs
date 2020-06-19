using System.Collections.Generic;
using System.Linq;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using OsmSharp.Tags;
using Xunit;

namespace ANYWAYS.UrbanisticPolygons.Tests.Graphs.Barrier
{
    public class TiledBarrierGraphExtensionTests
    {
        [Fact]
        public void TiledBarrierGraph_Flatten_NoData_ShouldDoNothing()
        {
            var graph = new TiledBarrierGraph();
            
            graph.Flatten();
        }

        [Fact]
        public void TiledBarrierGraph_Flatten_1Edge_ShouldChangeNothing()
        {
            var graphs = new TiledBarrierGraph();
            var v1 = graphs.AddVertex(4.7522735595703125, 50.97918242660188, 564341430);
            var v2 = graphs.AddVertex(4.7525310516357420, 50.97851368626033, 564341431);
            var e = graphs.AddEdge(v1, v2, Enumerable.Empty<(double longitude, double latitude)>(),
                new TagsCollection());
            
            graphs.Flatten();

            var enumerator = graphs.GetEnumerator();
            Assert.True(enumerator.MoveTo(v1));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(v1, enumerator.Vertex1);
            Assert.Equal(v2, enumerator.Vertex2);
            Assert.Equal(0, enumerator.Edge);
            Assert.Empty(enumerator.Shape);
            Assert.Equal(0, enumerator.Tags.Count);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void TiledBarrierGraph_Flatten_2Edges_OneIntersection_ShouldAddVertex()
        {
            var graphs = new TiledBarrierGraph();
            var v1 = graphs.AddVertex(4.7522735595703125, 50.97918242660188, 564341430);
            var v2 = graphs.AddVertex(4.7525310516357420, 50.97851368626033, 564341431);
            var v3 = graphs.AddVertex(4.752981662750244, 50.978996666362015, 564341432);
            var v4 = graphs.AddVertex(4.7518390417099, 50.97881090537897, 564341433);
            var e1 = graphs.AddEdge(v1, v2);
            var e2 = graphs.AddEdge(v3, v4);

            graphs.Flatten();

            var v5 = v4 + 1;
            var v5location = graphs.GetVertex(v5);
            Assert.Equal(4.7523825856415158, v5location.longitude, 7);
            Assert.Equal(50.978899271732737, v5location.latitude, 7);

            var expected = new HashSet<int>(new [] {e2 + 1, e2 + 2,e2 + 3,e2 + 4});
            var enumerator = graphs.GetEnumerator();
            Assert.True(enumerator.MoveTo(v1));
            Assert.True(enumerator.MoveNext());
            Assert.True(expected.Remove(enumerator.Edge));
            Assert.False(enumerator.MoveNext());
                
            Assert.True(enumerator.MoveTo(v2));
            Assert.True(enumerator.MoveNext());
            Assert.True(expected.Remove(enumerator.Edge));
            Assert.False(enumerator.MoveNext());
                
            Assert.True(enumerator.MoveTo(v3));
            Assert.True(enumerator.MoveNext());
            Assert.True(expected.Remove(enumerator.Edge));
            Assert.False(enumerator.MoveNext());
                
            Assert.True(enumerator.MoveTo(v4));
            Assert.True(enumerator.MoveNext());
            Assert.True(expected.Remove(enumerator.Edge));
            Assert.False(enumerator.MoveNext());
            Assert.Empty(expected);
            
            expected = new HashSet<int>(new [] {e2 + 1, e2 + 2,e2 + 3,e2 + 4});
            Assert.True(enumerator.MoveTo(v4 + 1));
            Assert.True(enumerator.MoveNext());
            Assert.True(expected.Remove(enumerator.Edge));
            Assert.True(enumerator.MoveNext());
            Assert.True(expected.Remove(enumerator.Edge));
            Assert.True(enumerator.MoveNext());
            Assert.True(expected.Remove(enumerator.Edge));
            Assert.True(enumerator.MoveNext());
            Assert.True(expected.Remove(enumerator.Edge));
            Assert.False(enumerator.MoveNext());
            Assert.Empty(expected);
        }
    }
}