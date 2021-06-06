using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace HealthCheckAPI
{
    public class HealthInspector : IHealthCheck
    {
        private readonly IConfiguration configuration;

        public HealthInspector(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            return DoHealthCheck();
        }

        private async Task<HealthCheckResult> DoHealthCheck()
        {
            var healthCheckData = new Dictionary<string, object>();

            // Check File IO Ops
            string filePath = @$"C:\Temp\healthcheck.txt";
            try
            {
                var file = File.Create(filePath);
                file.Close();
                healthCheckData.Add("File IO", "Healthy");
            }
            catch (Exception ex)
            {
                healthCheckData.Add("File IO", $"Unhealthy - Err: {ex.Message}");
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }

            // Check 3rd Party APIs
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    await client.GetStringAsync("http://www.google.com/");
                    healthCheckData.Add("3rd Party API", "Healthy");
                }
            }
            catch (Exception ex)
            {
                healthCheckData.Add("3rd Party API", $"Unhealthy - Err: {ex.Message}");
            }

            // Check the App Settings
            try
            {
                var appSetting = configuration.GetValue<string>("MyAppVariable");
                healthCheckData.Add("App Settings", string.IsNullOrEmpty(appSetting) ? "Unhealthy" : "Healthy");
            }
            catch (Exception ex)
            {
                healthCheckData.Add("App Settings", $"Unhealth - Err: {ex.Message}");
            }

            // Check the Environment variables
            try
            {
                string myAppEnv = Environment.GetEnvironmentVariable("MyApp", EnvironmentVariableTarget.User);
                healthCheckData.Add("Environment Variables", string.IsNullOrEmpty(myAppEnv) ? "Unhealthy" : "Healthy");
            }
            catch (Exception ex)
            {
                healthCheckData.Add("Environment Variables", $"Unhealth - Err: {ex.Message}");
            }
            
            var healthResult = new HealthCheckResult(HealthStatus.Healthy, "Application health", null, healthCheckData);

            return healthResult;
        }
    }
}
