using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using NetTopologySuite.Features;
using Newtonsoft.Json;

namespace ANYWAYS.UrbanisticPolygons.API.Formatters
{
    public class GeoJsonOutputFormatter : TextOutputFormatter
    {
        private readonly JsonSerializer _serializer =
            NetTopologySuite.IO.GeoJsonSerializer.Create();

        public GeoJsonOutputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/geo+json"));

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            if (context.ContentType == null ||
                string.IsNullOrEmpty(context.ContentType.Value)) return true;

            var parsedContentType = new MediaType(context.ContentType);
            foreach (var supported in SupportedMediaTypes)
            {
                if (context.ContentType.Equals(supported)) return true;
            }

            return false;
        }

        protected override bool CanWriteType(Type type)
        {
            return typeof(IEnumerable<Feature>).IsAssignableFrom(type);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context,
            Encoding selectedEncoding)
        {
            var response = context.HttpContext.Response;

            var features = new FeatureCollection();
            if (context.Object is IEnumerable<Feature> fs)
            {
                foreach (var f in fs)
                {
                    features.Add(f);
                }
            }
            else if (context.Object is IAsyncEnumerable<Feature> afs)
            {
                await foreach (var f in afs)
                {
                    features.Add(f);
                }
            }
            else
            {
                throw new ArgumentException("Cannot write response body for the given type.");
            }

            var textWriter = new StringWriter();
            _serializer.Serialize(textWriter, features);

            await response.WriteAsync(textWriter.ToString());
        }
    }
}