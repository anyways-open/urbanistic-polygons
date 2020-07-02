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
        public IAsyncEnumerable<Feature> Get(int z, uint x, uint y)
        {
            return TiledPolygonGraphBuilder.GetPolygonsForTile((x, y, z), Startup.CachePath, Startup.TileSource.GetTile,
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
                maxzoom = 14,
                minzoom = 11,
                attribution = "ANYWAYS BV",
                basename = "urban-polygons",
                id = "urban-polygons",
                vector_layers = new VectorLayer[]
                {
                    new VectorLayer
                    {
                        description = "Urban Polygons",
                        id = "urban-polygons",
                        maxzoom = 14,
                        minzoom = 11
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
        public async Task<IActionResult> GetMvt(int z, uint x, uint y)
        {
            if (z < 11) return NotFound();
            
            try
            {
                var polygons = TiledPolygonGraphBuilder.GetPolygonsForTile((x, y, z), Startup.CachePath, Startup.TileSource.GetTile,
                    IsBarrier);

                var layer = new Layer {Name = "urban-polygons"};
                await foreach (var loc in polygons)
                {
                    var max = double.MinValue;
                    var maxType = string.Empty;
                    
                    var names = loc.Attributes.GetNames();
                    var values = loc.Attributes.GetValues();
                    var attributes = new AttributesTable();
                    for (var a = 0; a < names.Length; a++)
                    {
                        attributes.Add(names[a], values[a]);
                        
                        var name = names[a];
                        if (name.StartsWith("face")) continue;

                        var o = values[a];
                        var d = o is IConvertible convertible ? convertible.ToDouble(null) : 0d;

                        if (d > max)
                        {
                            max = d;
                            maxType = name;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(maxType))
                    {
                        attributes.Add("type", maxType);
                    }
                    
                    layer.Features.Add(new Feature(loc.Geometry, attributes));
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
