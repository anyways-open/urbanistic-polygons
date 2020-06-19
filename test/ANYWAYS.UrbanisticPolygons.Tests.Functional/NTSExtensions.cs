using System.Collections.Generic;
using ANYWAYS.UrbanisticPolygons.Tiles;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace ANYWAYS.UrbanisticPolygons.Tests.Functional
{
    internal static class NTSExtensions
    {
        public static void AddRange(this FeatureCollection featureCollection, IEnumerable<Feature> features)
        {
            foreach (var feature in features)
            {
                featureCollection.Add(feature);
            }
        }

        public static string ToGeoJson(this FeatureCollection featureCollection)
        {
            return (new GeoJsonWriter()).Write(featureCollection);
        }

        public static Feature ToBboxFeature(int zoom, uint tileId)
        {
            var box = TileStatic.Box(zoom, tileId);
            var polygon = new Polygon(new LinearRing(new []
            {
                new Coordinate(box.left, box.top), 
                new Coordinate(box.right, box.top), 
                new Coordinate(box.right, box.bottom), 
                new Coordinate(box.left, box.bottom), 
                new Coordinate(box.left, box.top)
            }));
            
            return new Feature(polygon, new AttributesTable{{"tile_id", tileId},{"zoom", zoom}});
        }
    }
}