using ANYWAYS.UrbanisticPolygons.Tiles;

namespace ANYWAYS.UrbanisticPolygons.Guids
{
    internal static class GeoExtensions
    {
        public static int CompareTopLeft(this (int x, int y, uint tileId) location,
            (int x, int y, uint tileId) other)
        {
            return location.FromLocalTileCoordinates(14, 16384).CompareTopLeft(
                other.FromLocalTileCoordinates(14, 16384));
        }
        
        public static int CompareTopLeft(this (double longitude, double latitude) location,
            (double longitude, double latitude) other)
        {
            var lon = location.longitude.CompareTo(other.longitude);
            if (lon != 0) return lon;

            return -location.latitude.CompareTo(other.latitude);
        }
    }
}