using System.Collections.Generic;
using System.Linq;
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

        public  IEnumerable<T> Values()
        {
            return _barriers.Values.SelectMany(d => d.Values);
        }

        public T CalculateOrDefault(TagsCollectionBase tags, T defaultValue)
        {
            if (TryCalculateValue(tags, out var v))
            {
                return v;
            }

            return defaultValue;
        }


        /// <summary>
        /// Calculates the resistance for the given tag combination, returns null if this is not a barrier
        /// </summary>
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