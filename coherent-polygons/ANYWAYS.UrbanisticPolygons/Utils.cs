using System;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using OsmSharp;
using OsmSharp.Complete;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons
{
    public static class Utils
    {
        public static string TagsToString(this TagsCollectionBase tags)
        {
            if (tags == null)
            {
                return "{no tags given}";
            }

            var contents = string.Join(", ",
                tags.Select(t => t.Key + "=" + t.Value)
            );
            return "{" + contents + "}";
        }

        public static double GetAzimuth(Node departure, Node destination)
        {
            // https://stackoverflow.com/questions/642555/how-do-i-calculate-the-azimuth-angle-to-north-between-two-wgs84-coordinates
            var longitudinalDifference = destination.Longitude.Value - departure.Longitude.Value;
            var latitudinalDifference = destination.Latitude.Value - departure.Latitude.Value;
            var azimuth = (Math.PI * .5d) - Math.Atan(latitudinalDifference / longitudinalDifference);
            if (longitudinalDifference > 0) return azimuth;
            if (longitudinalDifference < 0) return azimuth + Math.PI;


            if (latitudinalDifference < 0) return Math.PI;
            return 0d;
        }
        
        public static T[] SubArray<T>(this T[] data, int start, int end)
        {
            var ts = new T[end - start];
            for (int i = start; i < end; i++)
            {
                ts[i - start] = data[i];
            }

            return ts;
        }
        
        public static T[] SubArray<T>(this T[] data, int start)
        {

            return data.SubArray(start, data.Length);
        }

        /// <summary>
        /// In radians
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static double GetDegreesAzimuth(Node departure, Node destination)
        {
            return GetAzimuth(departure, destination) * 180 / Math.PI;
        }


        public static string AsGeoJson(this IEnumerable<ICompleteOsmGeo> ways)
        {
            var empty = new Dictionary<string, object>();
            return ways.Select(w => (w, empty)).AsGeoJson();
        }
        
        public static string AsPolygonGeoJson(this IEnumerable<ICompleteOsmGeo> ways)
        {
            var empty = new Dictionary<string, object>();
            return ways.Select(w => (w, empty)).AsPolygonGeoJson();
        }

        public static string AsGeoJson(this IEnumerable<(ICompleteOsmGeo, Dictionary<string, object>)> ways)
        {
            var collection = new FeatureCollection();
            foreach (var (osmGeo, properties) in ways)
            {
                if (osmGeo is Node node)
                {
                    var coor =
                        new Coordinate(node.Longitude.Value, node.Latitude.Value);
                    collection.Add(new Feature(new Point(coor), new AttributesTable(properties)));
                }

                if (osmGeo is CompleteWay w)
                {
                    var coors = w.Nodes.Select(n =>
                        new Coordinate(n.Longitude.Value, n.Latitude.Value)).ToArray();
                    collection.Add(new Feature(new LineString(coors), new AttributesTable(properties)));
                }
            }

            var writer = new GeoJsonWriter();
            return writer.Write(collection);
        }
        
        public static string AsPolygonGeoJson(this IEnumerable<(ICompleteOsmGeo, Dictionary<string, object>)> ways)
        {
            var collection = new FeatureCollection();
            foreach (var (osmGeo, properties) in ways)
            {
                if (osmGeo is Node node)
                {
                    var coor =
                        new Coordinate(node.Longitude.Value, node.Latitude.Value);
                    collection.Add(new Feature(new Point(coor), new AttributesTable(properties)));
                }

                if (osmGeo is CompleteWay w)
                {
                    var coors = w.Nodes.Select(n =>
                        new Coordinate(n.Longitude.Value, n.Latitude.Value)).ToArray();
                    collection.Add(new Feature(new Polygon(
                        new LinearRing(coors)), new AttributesTable(properties)));
                }
            }

            var writer = new GeoJsonWriter();
            return writer.Write(collection);
        }

        public static string AsGeoJson(this CompleteWay way)
        {
            var coors = way.Nodes.Select(n =>
                new Coordinate(n.Longitude.Value, n.Latitude.Value)).ToArray();

            var writer = new GeoJsonWriter();
            return writer.Write(new Feature(new LineString(coors), new AttributesTable()));
        }
    }
}