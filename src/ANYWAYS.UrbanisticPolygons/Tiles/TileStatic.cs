using System;
using System.Collections.Generic;

namespace ANYWAYS.UrbanisticPolygons.Tiles
{
    internal static class TileStatic
    {
        public static (uint x, uint y) ToTile(int zoom, uint tileId)
        {
            var xMax = (ulong) (1 << zoom);

            return ((uint) (tileId % xMax), (uint) (tileId / xMax));
        }

        public static uint ToLocalId((uint x, uint y) tile, int zoom)
        {
            return ToLocalId(tile.x, tile.y, zoom);
        }

        public static uint ToLocalId(uint x, uint y, int zoom)
        {
            var xMax = (1 << (int) zoom);
            return (uint)(y * xMax + x);
        }

        public static ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) Box(int zoom, uint tileId)
        {
            var tile = ToTile(zoom, tileId);
            
            var n = Math.PI - ((2.0 * Math.PI * tile.y) / Math.Pow(2.0, zoom));
            var left = ((tile.x / Math.Pow(2.0, zoom) * 360.0) - 180.0);
            var top = (180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

            n = Math.PI - ((2.0 * Math.PI * (tile.y + 1)) / Math.Pow(2.0, zoom));
            var right = ((tile.x + 1) / Math.Pow(2.0, zoom) * 360.0) - 180.0;
            var bottom = (180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

            return ((left, top), (right, bottom));
        }

        public static (int x, int y, uint tileId) ToLocalTileCoordinates(int zoom, (double longitude, double latitude) location,
            int resolution)
        {
            var tileId = TileStatic.WorldTileLocalId(location, zoom);
            
            var tileLocation = ToLocalTileCoordinates(zoom, tileId, location.longitude, location.latitude, resolution);

            return (tileLocation.x, tileLocation.y, tileId);
        }

        public static (int x, int y) ToLocalTileCoordinates(int zoom, uint tileId, (double longitude, double latitude) location,
            int resolution)
        {
            return ToLocalTileCoordinates(zoom, tileId, location.longitude, location.latitude, resolution);
        }

        public static (int x, int y) ToLocalTileCoordinates(int zoom, uint tileId, double longitude, double latitude, int resolution)
        {
            var tile = ToTile(zoom, tileId);
            
            var n = Math.PI - ((2.0 * Math.PI * tile.y) / Math.Pow(2.0, zoom));
            var left = ((tile.x / Math.Pow(2.0, zoom) * 360.0) - 180.0);
            var top = (180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

            n = Math.PI - ((2.0 * Math.PI * (tile.y + 1)) / Math.Pow(2.0, zoom));
            var right = ((tile.x + 1) / Math.Pow(2.0, zoom) * 360.0) - 180.0;
            var bottom = (180.0 / Math.PI * Math.Atan(Math.Sinh(n)));
            
            var latStep = (top - bottom) / resolution;
            var lonStep = (right - left) / resolution;
            
            return ((int) ((longitude - left) / lonStep), (int) ((top - latitude) / latStep));
        }

        public static (double longitude, double latitude) FromLocalTileCoordinates(this (int x, int y, uint tileId) location, int zoom,
            int resolution)
        {
            FromLocalTileCoordinates(zoom, location.tileId, location.x, location.y, resolution, out var lon,
                out var lat);
            return (lon, lat);
        }

        public static void FromLocalTileCoordinates(int zoom, uint tileId, int x, int y, int resolution, out double longitude, out double latitude)
        {
            var tile = ToTile(zoom, tileId);
            
            var n = Math.PI - ((2.0 * Math.PI * tile.y) / Math.Pow(2.0, zoom));
            var left = ((tile.x / Math.Pow(2.0, zoom) * 360.0) - 180.0);
            var top = (180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

            n = Math.PI - ((2.0 * Math.PI * (tile.y + 1)) / Math.Pow(2.0, zoom));
            var right = ((tile.x + 1) / Math.Pow(2.0, zoom) * 360.0) - 180.0;
            var bottom = (180.0 / Math.PI * Math.Atan(Math.Sinh(n)));
            
            var latStep = (top - bottom) / resolution;
            var lonStep = (right - left) / resolution;

            longitude = left + (lonStep * x);
            latitude = top - (y * latStep);
        }

        public static uint WorldTileLocalId((double longitude, double latitude) coordinate, int zoom)
        {
            return WorldTileLocalId(coordinate.longitude, coordinate.latitude, zoom);
        }

        public static uint WorldTileLocalId(double longitude, double latitude, int zoom)
        {
            var tile = WorldToTile(longitude, latitude, zoom);
            return ToLocalId(tile, zoom);
        }
        
        public static (uint x, uint y) WorldToTile(double longitude, double latitude, int zoom)
        {
            var n = (int) Math.Floor(Math.Pow(2, zoom)); // replace by bit shifting?

            var rad = (latitude / 180d) * System.Math.PI;

            var x = (uint) ((longitude + 180.0f) / 360.0f * n);
            var y = (uint) (
                (1.0f - Math.Log(Math.Tan(rad) + 1.0f / Math.Cos(rad))
                 / Math.PI) / 2f * n);
            
            return (x, y);
        }

        public static IEnumerable<(uint x, uint y)> TilesFor(
            this ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight) box,
            int zoom)
        {
            var topLeft = TileStatic.WorldToTile(box.topLeft.longitude, box.topLeft.latitude, zoom);
            var bottomRight = TileStatic.WorldToTile(box.topLeft.longitude, box.topLeft.latitude, zoom);
            
            for (var x = topLeft.x; x <= bottomRight.x; x++)
            for (var y = topLeft.y; y <= topLeft.y; y++)
            {
                yield return (x, y);
            }
        }
    }
}