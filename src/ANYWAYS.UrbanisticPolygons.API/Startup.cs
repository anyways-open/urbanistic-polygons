using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ANYWAYS.UrbanisticPolygons.API.Formatters;
using ANYWAYS.UrbanisticPolygons.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ANYWAYS.UrbanisticPolygons.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
        internal static  string CachePath { get; private set; } 
        
        internal static IOsmTileSource TileSource { get; private set; } 

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(name: "CorsPolicy",
                    builder =>
                    {
                        builder.AllowAnyOrigin().AllowAnyMethod();
                    });
            });

            
            services.AddControllers(settings =>
            {
                // settings.RespectBrowserAcceptHeader = true;
                // settings.ReturnHttpNotAcceptable = true;
                    
                settings.OutputFormatters.Insert(0, new GeoJsonOutputFormatter());   
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            
            app.UseCors("CorsPolicy");

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            
            var cachePath = this.Configuration["cache_path"];
            var tileUrl = this.Configuration["tile_url"];

            Startup.CachePath = cachePath;
            Startup.TileSource = new OsmTileSource(tileUrl, cachePath);
        }
    }
}
