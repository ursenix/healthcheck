using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace HealthCheckAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddHealthChecks()
                    .AddCheck<HealthInspector>("MyApp Health")
                    .AddSqlServer(  @"Server=.\sqlexpress,1433;Database=HealthCheckDB;Integrated Security=False;User Id=sa;Password=YourStrongPassword;MultipleActiveResultSets=True", 
                                    // Following are the optional parameters
                                    "Select 1", // Default query that perform on the Server
                                    "SQL Health", 
                                    HealthStatus.Unhealthy, 
                                    null, 
                                    TimeSpan.FromSeconds(30));
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

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health", new HealthCheckOptions()
                {
                    ResponseWriter = WriteResponse
                });

            });
        }

        private static Task WriteResponse(HttpContext context, HealthReport result)
        {
            context.Response.ContentType = "application/json";

            JObject json;

            if (result.Status == HealthStatus.Healthy)
            {
                if (result.Entries.Count > 0)
                {
                    json = new JObject(
                            new JProperty("status", result.Status.ToString()),

                            new JProperty("results", new JObject(result.Entries.Select(pair =>
                                new JProperty(pair.Key, new JObject(
                                    new JProperty("status", pair.Value.Status.ToString()),
                                    new JProperty("description", pair.Value.Description),
                                    new JProperty("data", new JObject(pair.Value.Data.Select(
                                        p => new JProperty(p.Key, p.Value))))))))));
                }
                else
                {
                    json = new JObject(new JProperty("status", result.Status.ToString()));
                }

            }
            else
            {
                json = new JObject(
                new JProperty("status", result.Status.ToString()),

                new JProperty("results", new JObject(result.Entries.Select(pair =>
                    new JProperty(pair.Key, new JObject(
                        new JProperty("status", pair.Value.Status.ToString()),
                        new JProperty("description", pair.Value.Description),
                        new JProperty("data", new JObject(pair.Value.Data.Select(
                            p => new JProperty(p.Key, p.Value))))))))),

                new JProperty("exception", new JObject(result.Entries.Select(pair =>
                    new JProperty(pair.Key, new JObject(
                        new JProperty("message", (pair.Value.Exception == null) ? string.Empty : pair.Value.Exception.Message),
                        new JProperty("stackTrace", (pair.Value.Exception == null) ? string.Empty : pair.Value.Exception.StackTrace))))))
                );
            }

            return context.Response.WriteAsync(
                json.ToString(Formatting.Indented));
        }
    }
}
