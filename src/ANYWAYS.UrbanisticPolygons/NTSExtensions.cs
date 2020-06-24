using System.Collections.Generic;
using NetTopologySuite.Features;

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
    }
}