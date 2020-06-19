using System;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;
using OsmSharp;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons.Landuse
{
    internal static class LandusePolygons
    {
        public static IEnumerable<(Polygon polygon, string landuseType)> GetLandusePolygons(uint tile, Func<uint, IEnumerable<OsmGeo>> getTile,
            Func<TagsCollectionBase, string> getLanduseType)
        {
            throw new NotImplementedException();
        }
    }
}