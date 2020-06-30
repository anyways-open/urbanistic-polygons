using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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

        /// <summary>
        /// Gets the tile json for the location tiles.
        /// </summary>
        /// <returns></returns>
        [HttpGet("mvt.json")]
        [Produces("application/json")]
        public async Task<ActionResult<VectorTileSource>> GetMvtJson()
        {
            var mvt = new VectorTileSource
            {
                maxzoom = 20,
                minzoom = 6,
                attribution = "ANYWAYS BV",
                basename = "urban-polygons",
                id = "urban-polygons",
                vector_layers = new VectorLayer[]
                {
                    new VectorLayer
                    {
                        description = "Urban Polygons",
                        id = "urban-polygons",
                        maxzoom = 20,
                        minzoom = 6
                    }
                }
            };
            var url = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/{{z}}/{{x}}/{{y}}.mvt";
            mvt.tiles = new[]
            {
                url
            };
            return new JsonResult(mvt);
        }

        [HttpGet("{z}/{x}/{y}.mvt")]
        public FileStreamResult GetMvt(int z, uint x, uint y)
        {
            var tile = TileStatic.ToLocalId(x, y, z);

            try
            {
                var polygons = TiledPolygonGraphBuilder.GetPolygonsForTile(tile, Startup.CachePath, OsmTileSource.GetTile,
                    IsBarrier);

                var layer = new Layer {Name = "urban-polygons"};
                foreach (var loc in polygons)
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
