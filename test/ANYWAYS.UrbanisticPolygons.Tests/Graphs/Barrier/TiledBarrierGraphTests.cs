using System.Collections.Generic;
using System.Linq;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using OsmSharp.Tags;
using Xunit;

namespace ANYWAYS.UrbanisticPolygons.Tests.Graphs.Barrier
{
    public class TiledBarrierGraphTests
    {
        [Fact]
        public void TileBarrierGraph_AddVertex_1Vertex_ShouldAddVertex0()
        {
            var graphs = new TiledBarrierGraph();
            var v = graphs.AddVertex(4.7522735595703125, 50.97918242660188, 564341430);

            Assert.Equal(1, graphs.VertexCount);
            Assert.Equal(0, v);

            var vLocation = graphs.GetVertex(v);
            Assert.Equal((4.7522735595703125, 50.97918242660188), vLocation);
        }
        
        [Fact]
        public void TileBarrierGraph_Enumerator_1Vertex_ShouldEnumerate1Vertex()
        {
            var graphs = new TiledBarrierGraph();
            var v = graphs.AddVertex(4.7522735595703125, 50.97918242660188, 564341430);

            var enumerator = graphs.GetEnumerator();
            Assert.True(enumerator.MoveTo(v));
            Assert.False(enumerator.MoveNext());
        }
        
        [Fact]
        public void TileBarrierGraph_AddEdge_1Edge_ShouldAddEdge0()
        {
            var graphs = new TiledBarrierGraph();
            var v1 = graphs.AddVertex(4.7522735595703125, 50.97918242660188, 564341430);
            var v2 = graphs.AddVertex(4.7525310516357420, 50.97851368626033, 564341431);
            var e = graphs.AddEdge(v1, v2, Enumerable.Empty<(double longitude, double latitude)>(),
                new TagsCollection());
            
            Assert.Equal(2, graphs.VertexCount);
            Assert.Equal(0, e);

            var enumerator = graphs.GetEnumerator();
            Assert.True(enumerator.MoveTo(v1));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(v1, enumerator.Vertex1);
            Assert.Equal(v2, enumerator.Vertex2);
            Assert.Equal(0, enumerator.Edge);
            Assert.NotNull(enumerator.Shape);
            Assert.Empty(enumerator.Shape);
            Assert.Equal(0, enumerator.Tags.Count);
            Assert.False(enumerator.MoveNext());
        }
        
        [Fact]
        public void TileBarrierGraph_RemoveEdge_2Edges_RemoveLast_ShouldLeave1()
        {
            var graphs = new TiledBarrierGraph();
            var v1 = graphs.AddVertex(4.7522735595703125, 50.97918242660188, 564341430);
            var v2 = graphs.AddVertex(4.7525310516357420, 50.97851368626033, 564341431);
            var v3 = graphs.AddVertex(4.7525310516357420, 50.97851368626033, 564341432);
            var e1 = graphs.AddEdge(v1, v2, Enumerable.Empty<(double longitude, double latitude)>(),
                new TagsCollection());;
            var e2 = graphs.AddEdge(v1, v3, Enumerable.Empty<(double longitude, double latitude)>(),
                new TagsCollection());
            
            graphs.DeleteEdge(e2);

            var enumerator = graphs.GetEnumerator();
            Assert.True(enumerator.MoveTo(v1));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(v1, enumerator.Vertex1);
            Assert.Equal(v2, enumerator.Vertex2);
            Assert.Equal(0, enumerator.Edge);
            Assert.NotNull(enumerator.Shape);
            Assert.Empty(enumerator.Shape);
            Assert.Equal(0, enumerator.Tags.Count);
            Assert.False(enumerator.MoveNext());
            
            Assert.True(enumerator.MoveTo(v2));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(v1, enumerator.Vertex2);
            Assert.Equal(v2, enumerator.Vertex1);
            Assert.Equal(0, enumerator.Edge);
            Assert.NotNull(enumerator.Shape);
            Assert.Empty(enumerator.Shape);
            Assert.Equal(0, enumerator.Tags.Count);
            Assert.False(enumerator.MoveNext());
            
            Assert.True(enumerator.MoveTo(v3));
            Assert.False(enumerator.MoveNext());
        }
        
        [Fact]
        public void TileBarrierGraph_RemoveEdge_2Edges_RemoveFirst_ShouldLeave1()
        {
            var graphs = new TiledBarrierGraph();
            var v1 = graphs.AddVertex(4.7522735595703125, 50.97918242660188, 564341430);
            var v2 = graphs.AddVertex(4.7525310516357420, 50.97851368626033, 564341431);
            var v3 = graphs.AddVertex(4.7525310516357420, 50.97851368626033, 564341432);
            var e1 = graphs.AddEdge(v1, v2, Enumerable.Empty<(double longitude, double latitude)>(),
                new TagsCollection());;
            var e2 = graphs.AddEdge(v1, v3, Enumerable.Empty<(double longitude, double latitude)>(),
                new TagsCollection());
            
            graphs.DeleteEdge(e1);

            var enumerator = graphs.GetEnumerator();
            Assert.True(enumerator.MoveTo(v1));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(v1, enumerator.Vertex1);
            Assert.Equal(v3, enumerator.Vertex2);
            Assert.Equal(e2, enumerator.Edge);
            Assert.False(enumerator.MoveNext());
            
            Assert.True(enumerator.MoveTo(v2));
            Assert.False(enumerator.MoveNext());
            
            Assert.True(enumerator.MoveTo(v3));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(v1, enumerator.Vertex2);
            Assert.Equal(v3, enumerator.Vertex1);
            Assert.Equal(e2, enumerator.Edge);
            Assert.False(enumerator.MoveNext());
        }
        
        [Fact]
        public void TileBarrierGraph_RemoveEdge_2Edges_Add4_Remove2_ShouldLeave2()
        {
            var graphs = new TiledBarrierGraph();
            
            var v1 = graphs.AddVertex(4.7522735595703125, 50.97918242660188, 564341430);
            var v2 = graphs.AddVertex(4.7525310516357420, 50.97851368626033, 564341431);
            var v3 = graphs.AddVertex(4.752981662750244, 50.978996666362015, 564341432);
            var v4 = graphs.AddVertex(4.7518390417099, 50.97881090537897, 564341433);
            var e1 = graphs.AddEdge(v1, v2);
            var e2 = graphs.AddEdge(v3, v4);

            var v5 = graphs.AddVertex(4.752375483512878, 50.97889534228157);
            var e3 = graphs.AddEdge(v1, v5);
            var e4 = graphs.AddEdge(v5, v2);
            var e5 = graphs.AddEdge(v3, v5);
            var e6 = graphs.AddEdge(v5, v4);
            
            graphs.DeleteEdge(e1);
            graphs.DeleteEdge(e2);

            var enumerator = graphs.GetEnumerator();
            Assert.True(enumerator.MoveTo(v1));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(e3, enumerator.Edge);
            Assert.False(enumerator.MoveNext());
                
            Assert.True(enumerator.MoveTo(v2));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(e4, enumerator.Edge);
            Assert.False(enumerator.MoveNext());
                
            Assert.True(enumerator.MoveTo(v3));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(e5, enumerator.Edge);
            Assert.False(enumerator.MoveNext());
                
            Assert.True(enumerator.MoveTo(v4));
            Assert.True(enumerator.MoveNext());
            Assert.Equal(e6, enumerator.Edge);
            Assert.False(enumerator.MoveNext());
            
            var expected = new HashSet<int>(new [] {e3, e4,e5,e6});
            Assert.True(enumerator.MoveTo(v5));
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