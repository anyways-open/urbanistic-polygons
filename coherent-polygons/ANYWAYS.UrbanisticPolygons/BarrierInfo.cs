using System.Collections.Generic;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;

namespace ANYWAYS.UrbanisticPolygons
{
    public class WayWeight<T>
    {
        private Dictionary<string, Dictionary<string, T>>_barriers;


        public WayWeight(params (string key, string value, T resistance)[] barriers)
        {
            _barriers = new Dictionary<string, Dictionary<string, T>>();
            foreach (var (key, value, resistance) in barriers)
            {
                if (!_barriers.TryGetValue(key, out var nested))
                {
                    nested = new Dictionary<string, T>();
                    _barriers[key] = nested;
                }

                nested.Add(value, resistance);

            }
        }
        
        public Dictionary<Way, T> GetBarriers(XmlOsmStreamSource stream)
        {
            var allBarriers = new Dictionary<Way, T>();
            foreach (var feature in stream.EnumerateAndIgore(true, false, true))
            {
                if (!(feature is Way w))
                {
                    continue;
                }

                if (w.Tags == null)
                {
                    continue;
                }

                if (!TryCalculateValue(w.Tags, out var r))
                {
                    continue;
                }

                allBarriers.Add(w, r);
            }

            return allBarriers;
        }


        /// <summary>
        /// Calculates the resistance for the given tag combination, returns null if this is not a barrier
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public bool TryCalculateValue(TagsCollectionBase tags, out T value)
        {
            if (tags == null)
            {
                value = default;
                return false;
            }
            foreach (var tag in tags)
            {
                if (!_barriers.TryGetValue(tag.Key, out var nested))
                {
                    continue;
                }
                
                if (nested.TryGetValue(tag.Value, out value))
                {
                    return true;
                }

                if (nested.TryGetValue("*", out value))
                {
                    return true;
                }
                
            }

            value = default;
            return false;
        }
    }
}