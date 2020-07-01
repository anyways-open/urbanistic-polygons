using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ANYWAYS.UrbanisticPolygons.Landuse;
using ANYWAYS.UrbanisticPolygons.Tiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.IO.VectorTiles;
using NetTopologySuite.IO.VectorTiles.Mapbox;
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

            var landusePolygons = LandusePolygons.GetLandusePolygons(box, z, Startup.TileSource.GetTile, t =>
            {
                if (DefaultMergeFactorCalculator.Landuses.TryCalculateValue(t, out var type)) return type;

                return null;
            }).Select(p => new Feature(p.polygon, new AttributesTable{{"type", p.landuseType}}));

            return landusePolygons;
        }

        /// <summary>
        /// Gets the tile json for the location tiles.
        /// </summary>
        /// <returns></returns>
        [HttpGet("landuse/mvt.json")]
        [Produces("application/json")]
        public async Task<ActionResult<VectorTileSource>> GetMvtJson()
        {
            var mvt = new VectorTileSource
            {
                maxzoom = 14,
                minzoom = 14,
                attribution = "ANYWAYS BV",
                basename = "landuse",
                id = "landuse",
                vector_layers = new VectorLayer[]
                {
                    new VectorLayer
                    {
                        description = "landuse",
                        id = "landuse",
                        maxzoom = 14,
                        minzoom = 14
                    }
                }
            };
            var url = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/landuse/{{z}}/{{x}}/{{y}}.mvt";
            mvt.tiles = new[]
            {
                url
            };
            return new JsonResult(mvt);
        }

        [HttpGet("landuse/{z}/{x}/{y}.mvt")]
        public IActionResult GetMvt(int z, uint x, uint y)
        {
            if (z != 14) return NotFound();
            
            var tile = TileStatic.ToLocalId(x, y, z);

            try
            {
                var box = TileStatic.Box(z, tile);

                var landusePolygons = LandusePolygons.GetLandusePolygons(box, z, Startup.TileSource.GetTile, t =>
                {
                    if (DefaultMergeFactorCalculator.Landuses.TryCalculateValue(t, out var type)) return type;

                    return null;
                }).Select(p => new Feature(p.polygon, new AttributesTable{{"type", p.landuseType}}));

                var layer = new Layer {Name = "landuse"};
                foreach (var loc in landusePolygons)
                {
                    layer.Features.Add(loc);
                }

                var vectorTile = new VectorTile
                {
                    TileId = new NetTopologySuite.IO.VectorTiles.Tiles.Tile((int) x, (int) y, z).Id
                };
                vectorTile.Layers.Add(layer);

                var memoryStream = new MemoryStream();
                vectorTile.Write(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                return new FileStreamResult(memoryStream, "application/x-protobuf");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}