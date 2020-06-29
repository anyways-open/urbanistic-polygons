using System.Collections.Generic;
using System.Linq;
using ANYWAYS.UrbanisticPolygons.Landuse;
using ANYWAYS.UrbanisticPolygons.Tiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons.API.Controllers
{
    [ApiController]
    [Route("/")]
    public class LanduseTileController : ControllerBase
    {
        private readonly ILogger<LanduseTileController> _logger;

        public LanduseTileController(ILogger<LanduseTileController> logger)
        {
            _logger = logger;
        }
        
        bool IsBarrier(TagsCollectionBase? tags)
        {
            if (tags == null) return false;

            return DefaultMergeFactorCalculator.Barriers.TryCalculateValue(tags, out _);
        }

        [HttpGet("landuse/{z}/{x}/{y}")]
        public IEnumerable<Feature> Get(int z, uint x, uint y)
        {
            var tile = TileStatic.ToLocalId(x, y, z);
            var box = TileStatic.Box(z, tile);

            var landusePolygons = LandusePolygons.GetLandusePolygons(box, z, OsmTileSource.GetTile, t =>
            {
                if (DefaultMergeFactorCalculator.Landuses.TryCalculateValue(t, out var type)) return type;

                return null;
            }).Select(p => new Feature(p.polygon, new AttributesTable{{"type", p.landuseType}}));

            return landusePolygons;
        }
    }
}