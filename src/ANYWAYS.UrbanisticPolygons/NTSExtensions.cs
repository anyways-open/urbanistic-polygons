using System.Collections.Generic;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace ANYWAYS.UrbanisticPolygons
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
        
        public static FeatureCollection ToFeatureCollection(this IEnumerable<Feature> features)
        {
            var c = new FeatureCollection();
            c.AddRange(features);
            return c;
        }

        public static string ToGeoJson(this FeatureCollection features)
        {
            return (new GeoJsonWriter()).Write(features);
        }

        public static string ToGeoJson(this Geometry geometry)
        {
            var features = new FeatureCollection {new Feature(geometry, new AttributesTable())};
            return features.ToGeoJson();
        }
    }
}