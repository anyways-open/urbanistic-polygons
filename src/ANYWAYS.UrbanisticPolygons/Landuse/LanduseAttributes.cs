using System;
using System.Collections;
using System.Collections.Generic;

namespace ANYWAYS.UrbanisticPolygons.Landuse
{
    internal class LanduseAttributes : IEnumerable<(string type, double percentage)>
    {
        private readonly (string type, double percentage)[]? _data;

        public LanduseAttributes()
        {
            
        }
        
        internal LanduseAttributes((string type, double percentage)[] data)
        {
            _data = data;
        }

        public double Get(string type)
        {
            if (_data == null) return 0;
            
            for (var i = 0; i < _data.Length; i++)
            {
                if (_data[i].type == type)
                {
                    return _data[i].percentage;
                }
            }
            
            return 0;
        }

        public int Count => _data?.Length ?? 0;

        public LanduseAttributes Set(string type, double percentage)
        {
            if (_data == null)
            {
                return new LanduseAttributes(new (string type, double percentage)[]
                {
                    (type, percentage)
                });
            }
            var typeIndex = -1;
            for (var i = 0; i < _data.Length; i++)
            {
                if (_data[i].type != type) continue;
                
                typeIndex = i;
                break;
            }

            (string type, double percentage)[] data;
            if (typeIndex >= 0)
            {
                data = new (string type, double percentage)[_data.Length];
                _data.CopyTo((Span<(string type, double percentage)>) data);

                data[typeIndex] = (data[typeIndex].type, percentage);
            }
            else
            {
                data = new (string type, double percentage)[_data.Length + 1];
                _data.CopyTo((Span<(string type, double percentage)>) data);

                data[^1] = (type, percentage);
            }

            return new LanduseAttributes(data);
        }

        public IEnumerator<(string type, double percentage)> GetEnumerator()
        {
            if (_data == null) yield break;
            foreach (var d in _data)
            {
                yield return d;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}