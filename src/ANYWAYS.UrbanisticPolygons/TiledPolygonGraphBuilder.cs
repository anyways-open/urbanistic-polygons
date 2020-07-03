using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using ANYWAYS.UrbanisticPolygons.Graphs.Polygons;
using ANYWAYS.UrbanisticPolygons.Tiles;
using NetTopologySuite.Features;
using OsmSharp;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons
{
    public static class TiledPolygonGraphBuilder
    {
        public static async IAsyncEnumerable<Feature> GetPolygonsForTile((uint x, uint y, int zoom) tile, string folder,
            Func<uint, IEnumerable<OsmGeo>> getTile,
            Func<TagsCollectionBase, bool> isBarrier, bool build = true)
        {
            if (tile.zoom > 14) throw new ArgumentException("Zoom has to maximum 14.");

            if (tile.zoom == 14)
            {
                foreach (var (p, _) in await GetPolygonsForTile(TileStatic.ToLocalId(tile.x, tile.y, tile.zoom), folder,
                    getTile, isBarrier, build))
                {
                    yield return p;
                }
            }
            else 
            {
                var faceGuids = new HashSet<Guid>();

                foreach (var t in tile.SubTilesFor(14))
                {
                    var ps = await GetPolygonsForTile(TileStatic.ToLocalId(t.x, t.y, 14), folder, getTile, isBarrier, build);
                    foreach (var (p, id) in ps)
                    {
                        if (faceGuids.Contains(id)) continue;

                        faceGuids.Add(id);
                        yield return p;
                    }
                }
            }
        }

        private static async Task<IEnumerable<(Feature feature, Guid id)>> GetPolygonsForTile(uint tile, string folder,
            Func<uint, IEnumerable<OsmGeo>> getTile,
            Func<TagsCollectionBase, bool> isBarrier, bool build = true)
        {
            if (build) await TiledBarrierGraphBuilder.BuildForTile(tile, folder, getTile, isBarrier);

            var file = Path.Combine(folder, $"{tile}.tile.graph.zip");
            if (!File.Exists(file)) return Enumerable.Empty<(Feature feature, Guid id)>();
            
            await using var stream = File.OpenRead(file);
            await using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
            
            var polygonGraph = new TiledPolygonGraph();
            polygonGraph.AddTileFromStream(tile, gzipStream);

            return polygonGraph.GetAllPolygons();
        }
    }
}