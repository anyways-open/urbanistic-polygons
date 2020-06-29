using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using ANYWAYS.UrbanisticPolygons.Tiles;
using OsmSharp;
using OsmSharp.Streams;
using Serilog;

namespace ANYWAYS.UrbanisticPolygons.API
{
    internal static class OsmTileSource
    {
        internal static IEnumerable<OsmGeo> GetTile(uint t)
        {
            var z = 14;
            var (x, y) = TileStatic.ToTile(z, t);
            var tileUrl = Startup.TileUrl.Replace("{x}", x.ToString())
                .Replace("{y}", y.ToString())
                .Replace("{z}", z.ToString());
            var stream = Download(tileUrl);
            if (stream == null) return Enumerable.Empty<OsmGeo>();

            try
            {
                return (new XmlOsmStreamSource(stream)).ToList();
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to parse tile: {14}{x}/{y}");
                return Enumerable.Empty<OsmGeo>();
            }
        }

        private static Stream? Download(string url)
        {
            var fileName = HttpUtility.UrlEncode(url) + ".tile.zip";
            fileName = Path.Combine(Startup.CachePath, fileName);

            if (File.Exists(fileName))
            {
                return new GZipStream(File.OpenRead(fileName), CompressionMode.Decompress);
            }
            
            var redirectFileName = HttpUtility.UrlEncode(url) + ".tile.redirect";
            redirectFileName = Path.Combine(Startup.CachePath, redirectFileName);

            if (File.Exists(redirectFileName))
            {
                var newUrl = File.ReadAllText(redirectFileName);
                return Download(newUrl);
            }
                
            try
            {
                var handler = new HttpClientHandler {AllowAutoRedirect = false};

                var client = new HttpClient(handler);
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                var response = client.GetAsync(url);
                switch (response.Result.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        return null;
                    case HttpStatusCode.Moved:
                    {
                        return Download(response.Result.Headers.Location.ToString());
                    }
                    case HttpStatusCode.Redirect:
                    {
                        var uri = new Uri(url);
                        var redirected = new Uri($"{uri.Scheme}://{uri.Host}{response.Result.Headers.Location}");

                        using var stream = File.Open(redirectFileName, FileMode.Create);
                        using var streamWriter = new StreamWriter(stream);
                        streamWriter.Write(redirected);
                        
                        return Download(redirected.ToString());
                    }
                }

                var temp =  Path.Combine(Startup.CachePath, $"{Guid.NewGuid()}.temp");
                using (var stream = response.GetAwaiter().GetResult().Content.ReadAsStreamAsync().GetAwaiter()
                    .GetResult())
                using (var fileStream = File.Open(temp, FileMode.Create))
                {
                    stream.CopyTo(fileStream);    
                }
                
                if (File.Exists(fileName)) File.Delete(fileName);
                File.Move(temp, fileName);
                
                Log.Verbose($"Downloaded from {url}.");
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to download from {url}: {ex}.");
                return null;
            }
            
            return new GZipStream(File.OpenRead(fileName), CompressionMode.Decompress);
        }
    }
}