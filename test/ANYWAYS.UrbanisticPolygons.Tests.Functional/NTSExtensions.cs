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
    }
}