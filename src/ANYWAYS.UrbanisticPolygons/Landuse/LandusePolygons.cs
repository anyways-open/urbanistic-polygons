using System;
using System.Collections.Generic;
using ANYWAYS.UrbanisticPolygons.Tiles;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using OsmSharp;
using OsmSharp.Complete;
using OsmSharp.Streams;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons.Landuse
{
    public static class LandusePolygons
    {
        public static IEnumerable<(Polygon polygon, string landuseType)> GetLandusePolygons(
            ((double lon, double lat) tl, (double lon, double lat) br) box,
            int zoom,
            Func<uint, IEnumerable<OsmGeo>> getTile,
            Func<TagsCollectionBase, string?> getLanduseType)
        {
            var tiles = box.TilesFor(zoom);
            var ways = new HashSet<long>();
            foreach (var (x, y) in tiles)
            {
                var tile = TileStatic.ToLocalId(x, y, zoom);
                
                var stream = getTile(tile);
            
                foreach (var geo in stream.ToComplete())
                {
                    if (!(geo is CompleteWay w) || w.Tags == null) continue;

                    var landuseType = getLanduseType(w.Tags);
                    if (landuseType == null) continue;

                    var polygon = w.ToPolygon();
                    if(polygon == null) continue;
                    
                    if (ways.Contains(w.Id)) continue;
                    ways.Add(w.Id);

                    yield return (polygon, landuseType);
                }
            }
        }

        public static IEnumerable<(Polygon polygon, string landuseType, long id)> GetLandusePolygons(uint tile, Func<uint, IEnumerable<OsmGeo>> getTile,
            Func<TagsCollectionBase, string?> getLanduseType)
        {
            var stream = getTile(tile);
            
            foreach (var geo in stream.ToComplete())
            {
                if (!(geo is CompleteWay w) || w.Tags == null) continue;

                var landuseType = getLanduseType(w.Tags);
                if (landuseType == null) continue;

                var polygon = w.ToPolygon();
                if(polygon == null) continue;

                yield return (polygon, landuseType, w.Id);
            }
        }
    }
}