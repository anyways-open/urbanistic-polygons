using System.Collections.Generic;
using ANYWAYS.UrbanisticPolygons.Tiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons.API.Controllers
{
    [ApiController]
    [Route("/")]
    public class TileController : ControllerBase
    {
        private readonly ILogger<TileController> _logger;

        public TileController(ILogger<TileController> logger)
        {
            _logger = logger;
        }
        
        bool IsBarrier(TagsCollectionBase? tags)
        {
            if (tags == null) return false;

            return DefaultMergeFactorCalculator.Barriers.TryCalculateValue(tags, out _);
        }

        [HttpGet("{z}/{x}/{y}")]
        public IEnumerable<Feature> Get(int z, uint x, uint y)
        {
            var tile = TileStatic.ToLocalId(x, y, z);

            return TiledPolygonGraphBuilder.GetPolygonsForTile(tile, Startup.CachePath, OsmTileSource.GetTile,
                IsBarrier);
        }
    }
}
