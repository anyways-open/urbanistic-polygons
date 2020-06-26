using System;
using System.IO;

namespace ANYWAYS.UrbanisticPolygons.IO
{
    internal static class CustomBitCoder
    {
        internal static byte[] GetBytes(this (int x, int y, uint tileId) tiledLocation)
        {
            var idBytes = new byte[12];
            var idPartBytes = BitConverter.GetBytes(tiledLocation.tileId);
            idPartBytes.CopyTo(idBytes, 0);
            idPartBytes = BitConverter.GetBytes(tiledLocation.x);
            idPartBytes.CopyTo(idBytes, 4);
            idPartBytes = BitConverter.GetBytes(tiledLocation.y);
            idPartBytes.CopyTo(idBytes, 8);

            return idBytes;
        }

        internal static (int x, int y, uint tileId) ReadTiledLocation(this Stream stream)
        {
            var tileId = stream.ReadUInt32();
            var x = stream.ReadInt32();
            var y = stream.ReadInt32();
            return (x, y, tileId);
        }

        internal static Guid ReadGuid(this Stream stream)
        {
            var data = new byte[16];
            stream.Read(data, 0, 16);
            return new Guid(data);
        }

        internal static Guid? ReadNullableGuid(this Stream stream)
        {
            var data = new byte[16];
            stream.Read(data, 0, 16);
            foreach (var b in data)
            {
                if (b != 255) return new Guid(data);
            }

            return null;
        }
    }
}