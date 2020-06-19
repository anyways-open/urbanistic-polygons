// using System;
// using System.Collections.Generic;
// using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
// using ANYWAYS.UrbanisticPolygons.Graphs.Polygon;
// using OsmSharp;
// using OsmSharp.Tags;
//
// namespace ANYWAYS.UrbanisticPolygons
// {
//     internal static class Polygonizer
//     {
//         public static PolygonGraph GetPolygonsGraph(uint tile, Func<uint, IEnumerable<OsmGeo>> getTile,
//             Func<TagsCollectionBase, bool> isBarrier)
//         {
//             // load the tile in the barrier graph.
//             var tileData = getTile(tile);
//             var barrierGraph = new TiledBarrierGraph(14);
//             barrierGraph.AddTile(tile, tileData, isBarrier);
//             
//             // create the dual polygon graph.
//             var polygonGraph = new PolygonGraph();
//             
//             // find edge without assigned face on either side and start there.
//             
//             
//             // loop over all vertices and try to convert the neighbours to polygons.
//             
//             throw new NotImplementedException();
//         }
//         
//         private static void PrepareVertex()
//     }
// }