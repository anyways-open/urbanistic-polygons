using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ANYWAYS.UrbanisticPolygons.Graphs.Polygons;
using NetTopologySuite.Features;
using OsmSharp;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons
{
    public static class TiledPolygonGraphBuilder
    {
        public static IEnumerable<Feature> GetPolygonsForTile(uint tile, string folder, Func<uint, IEnumerable<OsmGeo>> getTile,
            Func<TagsCollectionBase, bool> isBarrier)
        {
            TiledBarrierGraphBuilder.BuildForTile(tile, folder, getTile, isBarrier);

            using var stream = File.OpenRead(Path.Combine(folder, $"{tile}.tile.graph.zip"));
            using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
            
            var polygonGraph = new TiledPolygonGraph();
            polygonGraph.AddTileFromStream(tile, gzipStream);

            return polygonGraph.GetAllPolygons();
        }
    }
}