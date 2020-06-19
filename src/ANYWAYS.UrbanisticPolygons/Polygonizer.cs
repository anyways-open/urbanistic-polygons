// using System;
// using System.Collections.Generic;
// using System.Linq;
// using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
// using ANYWAYS.UrbanisticPolygons.Graphs.Polygon;
// using OsmSharp;
// using OsmSharp.Tags;
//
// namespace ANYWAYS.UrbanisticPolygons
// {
//     internal static class Polygonizer
//     {
//         public static IEnumerable<object> GetPolygons(uint tile, Func<uint, IEnumerable<OsmGeo>> getTile,
//             Func<TagsCollectionBase, bool> isBarrier)
//         {
//             // load the tile in the barrier graph.
//             var tileData = getTile(tile);
//             var barrierGraph = new TiledBarrierGraph(14);
//             barrierGraph.AddTile(tile, tileData, isBarrier);
//             
//             // make sure the graph is complete enough to calculate all faces.
//             var missingTiles = new HashSet<uint>();
//             for 
//             
//             // try to determine all the faces for all edges that have at least one vertex in the requested tile.
//             // queue all undetermined edges because they are not fully loaded, and repeat until done.
//             var incompleteEdges = new HashSet<uint>();
//             while (true)
//             {
//                 break;
//             }
//             
//             // for each vertex in the graph:
//             // - when the vertex is not in a loaded tile:
//             //   - mark both edge faces as unknown.
//             
//             // for each vertex in the given tile:
//             // - determine the faces for each edge starting at the vertex.
//             // - 
//             
//             return Enumerable.Empty<object>();
//         }
//     }
// }