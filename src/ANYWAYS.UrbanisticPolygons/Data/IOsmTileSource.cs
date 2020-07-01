using System.Collections.Generic;
using OsmSharp;

namespace ANYWAYS.UrbanisticPolygons.Data
{
    /// <summary>
    /// Abstract representation of an OSM tile source.
    /// </summary>
    public interface IOsmTileSource
    {
        /// <summary>
        /// Gets the given tile.
        /// </summary>
        /// <param name="tileId">The tile id at zoom level 14.</param>
        /// <returns>The OSM data at the given tile location.</returns>
        IEnumerable<OsmGeo> GetTile(uint tileId);
    }
}