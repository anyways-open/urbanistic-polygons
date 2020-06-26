using System.IO;
using ANYWAYS.UrbanisticPolygons.IO;

namespace ANYWAYS.UrbanisticPolygons.Landuse
{
    internal static class LanduseAttributesExtensions
    {
        public static void WriteAttributes(this Stream stream, LanduseAttributes attributes)
        {
            stream.WriteInt32(attributes.Count);
            foreach (var (t, p) in attributes)
            {
                stream.WriteWithSize(t);
                stream.WriteDouble(p);
            }
        }

        public static LanduseAttributes ReadAttributes(this Stream stream)
        {
            var buffer = new byte[1024];
            var c = stream.ReadInt32();
            var data = new (string type, double percentage)[c];
            for (var i = 0; i < c; i++)
            {
                data[i] = (stream.ReadWithSizeString(buffer), stream.ReadDouble())!;
            }
            
            return new LanduseAttributes(data);
        }
    }
}