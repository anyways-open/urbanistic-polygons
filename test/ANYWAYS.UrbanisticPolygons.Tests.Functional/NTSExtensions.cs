using System.Collections.Generic;
using NetTopologySuite.Features;
using NetTopologySuite.IO;

namespace ANYWAYS.UrbanisticPolygons.Tests.Functional
{
    internal static class NTSExtensions
    {
        public static FeatureCollection ToFeatureCollection(this IEnumerable<Feature> features)
        {
            var c = new FeatureCollection();
            c.AddRange(features);
            return c;
        }
        
        public static string ToGeoJson(this FeatureCollection featureCollection)
        {
            return (new GeoJsonWriter()).Write(featureCollection);
        }

        public static FeatureCollection FromGeoJson(this string geojson)
        {
            var reader = new GeoJsonReader();
            return reader.Read<FeatureCollection>(geojson);
        }
    }
}