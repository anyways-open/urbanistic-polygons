using System;
using System.Collections.Generic;
using System.Linq;
using ANYWAYS.UrbanisticPolygons.Geo;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier;
using ANYWAYS.UrbanisticPolygons.Graphs.Barrier.Faces;
using ANYWAYS.UrbanisticPolygons.Tiles;
using NetTopologySuite.Geometries;
using OsmSharp.Logging;

namespace ANYWAYS.UrbanisticPolygons.Landuse
{
    internal static class TiledBarrierGraphExtensions
    {
        public static void AssignLanduse(
            this TiledBarrierGraph tiledBarrierGraph, uint tile,
            Func<((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight), IEnumerable<(Polygon polygon, string type)>> getLanduse)
        {
            var tileBox = TileStatic.Box(tiledBarrierGraph.Zoom, tile);
            var outerBox = tiledBarrierGraph.OuterBox(tile);

            // get all landuse polygon in the larger box.
            var landuse = getLanduse(outerBox).ToList();
            
            for (var f = 1; f < tiledBarrierGraph.FaceCount; f++)
            {
                // determine if face overlaps with tile.
                var box = tiledBarrierGraph.FaceToClockwiseCoordinates(f).ToBox();
                if (box == null) continue;
                if (!box.Value.Overlaps(tileBox)) continue; // face is not in tile.
                
                // build face polygon.
                var facePolygon = tiledBarrierGraph.ToPolygon(f);
                if (facePolygon == null) continue; // face is not a polygon.
                
                // build the overlap per type.
                var types = new Dictionary<string, double>();
                
                // get all the polygons for all tiles for the current face.
                foreach (var (polygon, type) in landuse)
                {
                    var percentage = 0.0;
                    try
                    {
                        if (!polygon.EnvelopeInternal.Intersects(facePolygon.EnvelopeInternal)) continue;
                        
                        if (polygon.Covers(facePolygon))
                        {
                            // landuse completely overlaps the polygon, add it as 100%
                            percentage = 1;
                        }
                        else if (facePolygon.Covers(polygon))
                        {
                            // landuse completely inside face.
                            percentage = polygon.Area / facePolygon.Area;
                        }
                        else 
                        {
                            // partial overlap probably.
                            var intersection = facePolygon.Intersection(polygon);
                            if (intersection == null || intersection.IsEmpty) continue;

                            if (intersection is Polygon intersectionPolygon) 
                            {
                                percentage = intersectionPolygon.Area / facePolygon.Area;
                            }
                            else
                            {
                                OsmSharp.Logging.Logger.Log(nameof(TiledBarrierGraphBuilder), TraceEventType.Warning,
                                    $"Unhandled intersection type.");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // TODO: non-noded intersections, due to invalid polygons?
                        OsmSharp.Logging.Logger.Log(nameof(TiledBarrierGraphBuilder), TraceEventType.Warning,
                            $"Unhandled exception calculating intersections.");
                    }

                    // update stats.
                    if (!types.TryGetValue(type, out var overlap))
                    {
                        overlap = 0;
                    }
                    types[type] = percentage + overlap;
                }

                // set face data.
                var attributes = new LanduseAttributes();
                foreach (var pair in types)
                {
                    attributes = attributes.Set(pair.Key, pair.Value);
                }
                tiledBarrierGraph.SetFaceData(f, attributes);
            }
        }

        internal static ((double longitude, double latitude) topLeft, (double longitude, double latitude) bottomRight)
            OuterBox(this TiledBarrierGraph tiledBarrierGraph, uint tile)
        {
            var tileBox = TileStatic.Box(tiledBarrierGraph.Zoom, tile);
            var largerBox = tileBox;
            
            // expand box until we have the total outline.
            for (var f = 1; f < tiledBarrierGraph.FaceCount; f++)
            {
                // determine if face overlaps with tile.
                var box = tiledBarrierGraph.FaceToClockwiseCoordinates(f).ToBox();
                if (box == null) continue;
                if (!box.Value.Overlaps(tileBox)) continue; // face is not in tile.

                // expand the box if needed.
                if (largerBox.Overlaps(box.Value.bottomRight) &&
                    largerBox.Overlaps(box.Value.topLeft)) continue;
                largerBox = largerBox.Expand(box.Value);
            }

            return largerBox;
        }
    }
}