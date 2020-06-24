using System.Linq;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Faces;
using OsmSharp;
using Xunit;

namespace ANYWAYS.UrbanisticPolygons.Tests.Graphs.Barrier.Faces
{
    public class FacesTests
    {
        [Fact]
        public void Faces_AssignFaces_NoEdges_ShouldDoNothing()
        {
            var graphs = new TiledBarrierGraph();
            
            graphs.AssignFaces(41525);
        }

        [Fact]
        public void Faces_AssignFaces_OneEdge_ShouldDoNothing()
        {
            var graphs = new TiledBarrierGraph();
            
            var v1 = graphs.AddVertex(4.7522735595703125, 50.97918242660188, 564341430);
            var v2 = graphs.AddVertex(4.7525310516357420, 50.97851368626033, 564341431);
            var e = graphs.AddEdge(v1, v2);
            var tile = Tiles.TileStatic.WorldTileLocalId(4.7522735595703125, 50.97918242660188, 14);
            graphs.SetTileLoaded(tile);
            
            graphs.AssignFaces(tile);

            Assert.Equal(0, graphs.FaceCount);
        }
        
        [Fact]
        public void Faces_RightTurnLoop_3EdgeLoop_Forward_ShouldReturnClockwiseLoop()
        {
            var graphs = new TiledBarrierGraph();
            
            //    0
            //   / \
            //  1---2
            
            var v1 = graphs.AddVertex(
                4.788075685501099,
                51.26676188180721, 564341430);
            var v2 = graphs.AddVertex(
                4.786123037338257,
                51.26496276736555, 564341431);
            var v3 = graphs.AddVertex(
                4.790832996368408,
                51.265137311403734, 564341432);
            
            var e1 = graphs.AddEdge(v1, v2);
            var e2 = graphs.AddEdge(v2, v3);
            var e3 = graphs.AddEdge(v3, v1);
            graphs.SetTileLoaded(Tiles.TileStatic.WorldTileLocalId(graphs.GetVertex(v1), 14));
            graphs.SetTileLoaded(Tiles.TileStatic.WorldTileLocalId(graphs.GetVertex(v2), 14));
            graphs.SetTileLoaded(Tiles.TileStatic.WorldTileLocalId(graphs.GetVertex(v3), 14));
            
            // calculate right turn loop starting at e1.
            var enumerator = graphs.GetEnumerator();
            enumerator.MoveTo(v1);
            enumerator.MoveNextUntil(e1);
            var (loop, _) = enumerator.RightTurnLoop();
            
            Assert.NotNull(loop);
            Assert.Equal(3, loop.Count);
            Assert.Equal((v1, e1, true, v2), loop[0]);
            Assert.Equal((v2, e2, true, v3), loop[1]);
            Assert.Equal((v3, e3, true, v1), loop[2]);
        }
        
        [Fact]
        public void Faces_RightTurnLoop_3EdgeLoop_Backward_ShouldReturnCounterClockwiseLoop()
        {
            var graphs = new TiledBarrierGraph();
            
            //    0
            //   / \
            //  1---2
            
            var v1 = graphs.AddVertex(
                4.788075685501099,
                51.26676188180721, 564341430);
            var v2 = graphs.AddVertex(
                4.786123037338257,
                51.26496276736555, 564341431);
            var v3 = graphs.AddVertex(
                4.790832996368408,
                51.265137311403734, 564341432);
            
            var e1 = graphs.AddEdge(v1, v2);
            var e2 = graphs.AddEdge(v2, v3);
            var e3 = graphs.AddEdge(v3, v1);
            graphs.SetTileLoaded(Tiles.TileStatic.WorldTileLocalId(graphs.GetVertex(v1), 14));
            graphs.SetTileLoaded(Tiles.TileStatic.WorldTileLocalId(graphs.GetVertex(v2), 14));
            graphs.SetTileLoaded(Tiles.TileStatic.WorldTileLocalId(graphs.GetVertex(v3), 14));
            
            // calculate right turn loop starting at e1.
            var enumerator = graphs.GetEnumerator();
            enumerator.MoveTo(v2);
            enumerator.MoveNextUntil(e1);
            var (loop, _) = enumerator.RightTurnLoop();
            
            Assert.NotNull(loop);
            Assert.Equal(3, loop.Count);
            Assert.Equal((v2, e1, false, v1), loop[0]);
            Assert.Equal((v1, e3, false, v3), loop[1]);
            Assert.Equal((v3, e2, false, v2), loop[2]);
        }

        [Fact]
        public void Faces_AssignFaces_OneLoop1_ShouldAssign2()
        {
            var graphs = new TiledBarrierGraph();
            
            //    0
            //   / \
            //  1---2
            
            var v1 = graphs.AddVertex(
                4.788075685501099,
                51.26676188180721, 564341430);
            var v2 = graphs.AddVertex(
                4.786123037338257,
                51.26496276736555, 564341431);
            var v3 = graphs.AddVertex(
                4.790832996368408,
                51.265137311403734, 564341432);
            
            var e1 = graphs.AddEdge(v1, v2);
            var e2 = graphs.AddEdge(v2, v3);
            var e3 = graphs.AddEdge(v3, v1);
            graphs.SetTileLoaded(Tiles.TileStatic.WorldTileLocalId(graphs.GetVertex(v1), 14));
            graphs.SetTileLoaded(Tiles.TileStatic.WorldTileLocalId(graphs.GetVertex(v2), 14));
            graphs.SetTileLoaded(Tiles.TileStatic.WorldTileLocalId(graphs.GetVertex(v3), 14));
            
            graphs.AssignFaces(Tiles.TileStatic.WorldTileLocalId(graphs.GetVertex(v1), 14));

            Assert.Equal(2, graphs.FaceCount);
            var enumerator = graphs.GetEnumerator();
            enumerator.MoveTo(v1);
            enumerator.MoveNextUntil(e1);
            
            Assert.NotEqual(int.MaxValue, enumerator.FaceLeft);
            var left = enumerator.FaceLeft;
            Assert.NotEqual(int.MaxValue, enumerator.FaceRight);
            var right = enumerator.FaceRight;
            
            enumerator.MoveTo(v2);
            enumerator.MoveNextUntil(e2);
            Assert.Equal(left, enumerator.FaceLeft);
            Assert.Equal(right, enumerator.FaceRight);
            enumerator.MoveTo(v3);
            enumerator.MoveNextUntil(e3);
            Assert.Equal(left, enumerator.FaceLeft);
            Assert.Equal(right, enumerator.FaceRight);
        }

        [Fact]
        public void Faces_AssignFaces_OneLoop2_ShouldAssign2()
        {
            var graphs = new TiledBarrierGraph();
            
            //    0
            //   / \
            //  1---2
            
            var v1 = graphs.AddVertex(
                4.788075685501099,
                51.26676188180721, 564341430);
            var v2 = graphs.AddVertex(
                4.786123037338257,
                51.26496276736555, 564341431);
            var v3 = graphs.AddVertex(
                4.790832996368408,
                51.265137311403734, 564341432);
            
            var e1 = graphs.AddEdge(v1, v2);
            var e2 = graphs.AddEdge(v3, v2);
            var e3 = graphs.AddEdge(v3, v1);
            graphs.SetTileLoaded(Tiles.TileStatic.WorldTileLocalId(graphs.GetVertex(v1), 14));
            graphs.SetTileLoaded(Tiles.TileStatic.WorldTileLocalId(graphs.GetVertex(v2), 14));
            graphs.SetTileLoaded(Tiles.TileStatic.WorldTileLocalId(graphs.GetVertex(v3), 14));
            
            graphs.AssignFaces(Tiles.TileStatic.WorldTileLocalId(graphs.GetVertex(v1), 14));

            Assert.Equal(2, graphs.FaceCount);
            var enumerator = graphs.GetEnumerator();
            enumerator.MoveTo(v1);
            enumerator.MoveNextUntil(e1);
            
            Assert.NotEqual(int.MaxValue, enumerator.FaceLeft);
            var left = enumerator.FaceLeft;
            Assert.NotEqual(int.MaxValue, enumerator.FaceRight);
            var right = enumerator.FaceRight;
            
            enumerator.MoveTo(v2);
            enumerator.MoveNextUntil(e2);
            Assert.False(enumerator.Forward);
            Assert.Equal(left, enumerator.FaceRight);
            Assert.Equal(right, enumerator.FaceLeft);
            enumerator.MoveTo(v3);
            enumerator.MoveNextUntil(e3);
            Assert.Equal(left, enumerator.FaceLeft);
            Assert.Equal(right, enumerator.FaceRight);
        }
        
        
    }
}