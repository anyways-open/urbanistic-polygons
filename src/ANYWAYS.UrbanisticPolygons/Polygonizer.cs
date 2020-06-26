using System;
using System.Collections.Generic;
using System.Linq;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Faces;
using NetTopologySuite.Features;
using OsmSharp;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons
{
    internal static class Polygonizer
    {
        // public static IEnumerable<Feature> GetPolygons(uint tile, Func<uint, IEnumerable<OsmGeo>> getTile,
        //     Func<TagsCollectionBase, bool> isBarrier)
        // {
        //     // load data for tile.
        //     var graph = new TiledBarrierGraph();
        //     graph.LoadForTile(tile, getTile, isBarrier);
        //     
        //     // run face assignment for the tile.
        //     var result = graph.AssignFaces(tile);
        //     while (!result.success)
        //     {
        //         // extra tiles need loading.
        //         graph.AddTiles(result.missingTiles, getTile, isBarrier);
        //         
        //         // try again.
        //         result = graph.AssignFaces(tile);
        //     }
        //     
        //     // for every face, determine polygon.
        //     // TODO: filter for tile.
        //     for (var f = 0; f < graph.FaceCount; f++)
        //     {
        //         var polygon = graph.ToPolygon(f);
        //         if (polygon == null) continue;
        //         
        //         yield return polygon;
        //     }
        // }
    }
}