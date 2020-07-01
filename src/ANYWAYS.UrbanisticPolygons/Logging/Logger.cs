using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ANYWAYS.UrbanisticPolygons.Logging
{
    internal static class Logger
    {
        public static void SetLoggerFactor(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;
        }

        public static ILoggerFactory LoggerFactory { get; private set; } = new NullLoggerFactory();
    }
}