using System;
using System.Collections.Generic;
using System.Linq;

namespace ANYWAYS.UrbanisticPolygons
{
    public class DefaultMergeFactorCalculator
    {
        private readonly double _expectedPolygonSize;

        /// <summary>
        /// Barrier resistance for not-similar areas
        /// </summary>
        public static WayWeight<int> Barriers = new WayWeight<int>(
            ("highway", "living_street", 1),
            ("highway", "residential", 2),
            ("highway", "track", 1),
            ("highway", "unclassified", 2),
            ("highway", "tertiary", 4),
            ("highway", "tertiary_link", 4),
            ("highway", "secondary", 8),
            ("highway", "secondary_link", 8),
            ("highway", "primary", 16),
            ("highway", "primary_link", 16),
            ("highway", "motorway", 30),
            ("highway", "motorway_link", 30),
            ("highway", "trunk", 30),
            ("highway", "trunk_link", 30),
            ("railway", "*", 30),
            ("natural", "water", 10),
            ("waterway", "stream", 5)
        );

        public static WayWeight<string> BarrierClassification = new WayWeight<string>(
            ("highway", "service", "residential"),
            ("highway", "pedestrian", "residential"),
            ("highway", "living_street", "residential"),
            ("highway", "residential", "residential"),
            ("highway", "track", "rural"),
            ("highway", "unclassified", "rural"),
            ("railway", "*", "industrial"),
            ("waterway", "*", "water"),
            ("natural", "water", "water")
        );

        public static WayWeight<string> Landuses = new WayWeight<string>(
            ("landuse", "residential", "residential"),
            ("landuse", "industrial", "industrial"),
            ("amenity", "school", "school"),
            ("amenity", "college", "school"),
            ("amenity", "university", "school"),
            ("amenity", "kindergarten", "school"),
            ("landuse", "meadow", "rural"),
            ("landuse", "farmland", "rural"),
            ("landuse", "forest", "natural"),
            ("landuse", "grass", "natural"),
            ("leisure", "park", "natural"),
            ("landuse", "retail", "industrial"),
            ("natural", "water", "water"),
            ("waterway", "riverbank", "water")
        );
        
        public DefaultMergeFactorCalculator(double expectedPolygonSize)
        {
            _expectedPolygonSize = expectedPolygonSize;
        }
    }
}