using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ANYWAYS.UrbanisticPolygons.Data;
using ANYWAYS.UrbanisticPolygons.Tiles;
using OsmSharp.Tags;
using Serilog;
using Serilog.Formatting.Json;

namespace ANYWAYS.UrbanisticPolygons.Preprocessor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var logFile = Path.Combine("logs", "log-{Date}.txt");
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .Enrich.FromLogContext()
                .WriteTo.RollingFile(new JsonFormatter(), logFile)
                .WriteTo.Console()
                .CreateLogger();
            
            var cacheFolder = "/media/xivk/2T-SSD-EXT/temp";
            var tileUrl = "https://data1.anyways.eu/tiles/full/20200628-150902/14/{x}/{y}.osm";
            
            var osmTileSource = new OsmTileSource(tileUrl, cacheFolder);

            bool IsBarrier(TagsCollectionBase? tags)
            {
                if (tags == null) return false;

                return DefaultMergeFactorCalculator.Barriers.TryCalculateValue(tags, out _);
            }
            
            var box = ((4.239349365234375, 51.36749369152795), (5.16632080078125, 50.92554455650557));

            var tiles = box.TilesFor(14).Select(x => TileStatic.ToLocalId(x, 14));
            foreach (var tile in tiles)
            {
                Log.Information($"Processing tile {TileStatic.ToTile(14, tile)}...");
                await TiledBarrierGraphBuilder.BuildForTile(tile, cacheFolder, x =>
                {
                    //Log.Information($"Fetching OSM data tile {TileStatic.ToTile(14, x)}...");
                    return osmTileSource.GetTile(x);
                }, IsBarrier);
            }
        }
    }
}
