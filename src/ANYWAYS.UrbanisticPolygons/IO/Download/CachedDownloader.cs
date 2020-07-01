using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using ANYWAYS.UrbanisticPolygons.Logging;
using Microsoft.Extensions.Logging;

namespace ANYWAYS.UrbanisticPolygons.IO.Download
{
    internal class CachedDownloader
    {
        internal Stream? Download(string url, string cachePath)
        {
            var logger = Logger.LoggerFactory.CreateLogger<CachedDownloader>();
            
            var fileName = HttpUtility.UrlEncode(url) + ".tile.zip";
            fileName = Path.Combine(cachePath, fileName);

            if (File.Exists(fileName))
            {
                return new GZipStream(File.OpenRead(fileName), CompressionMode.Decompress);
            }
            
            var redirectFileName = HttpUtility.UrlEncode(url) + ".tile.redirect";
            redirectFileName = Path.Combine(cachePath, redirectFileName);

            if (File.Exists(redirectFileName))
            {
                var newUrl = File.ReadAllText(redirectFileName);
                return Download(newUrl, cachePath);
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
                        return Download(response.Result.Headers.Location.ToString(), cachePath);
                    }
                    case HttpStatusCode.Redirect:
                    {
                        var uri = new Uri(url);
                        var redirected = new Uri($"{uri.Scheme}://{uri.Host}{response.Result.Headers.Location}");

                        using var stream = File.Open(redirectFileName, FileMode.Create);
                        using var streamWriter = new StreamWriter(stream);
                        streamWriter.Write(redirected);
                        
                        return Download(redirected.ToString(), cachePath);
                    }
                }

                var temp =  Path.Combine(cachePath, $"{Guid.NewGuid()}.temp");
                using (var stream = response.GetAwaiter().GetResult().Content.ReadAsStreamAsync().GetAwaiter()
                    .GetResult())
                using (var fileStream = File.Open(temp, FileMode.Create))
                {
                    stream.CopyTo(fileStream);    
                }
                
                if (File.Exists(fileName)) File.Delete(fileName);
                File.Move(temp, fileName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to download from {url}.", url);
                return null;
            }
            
            return new GZipStream(File.OpenRead(fileName), CompressionMode.Decompress);
        }
    }
}