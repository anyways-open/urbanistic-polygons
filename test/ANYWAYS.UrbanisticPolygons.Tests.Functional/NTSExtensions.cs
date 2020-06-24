using NetTopologySuite.Features;
using NetTopologySuite.IO;

namespace ANYWAYS.UrbanisticPolygons.Tests.Functional
{
    internal static class NTSExtensions
    {
        public static string ToGeoJson(this FeatureCollection featureCollection)
        {
            return (new GeoJsonWriter()).Write(featureCollection);
        }
    }
}