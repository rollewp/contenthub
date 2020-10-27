using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;

namespace RDA.ContentHub.AzureFunctions
{
    public static class AppSettings
    {
        private static IConfiguration _config;
        public static IConfiguration Configuration
        {
            get
            {
                if (_config == null)
                {
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("local.settings.json", optional: true)
                        .AddEnvironmentVariables();
                    _config = builder.Build();
                }

                return _config;
            }
        }

        public static Uri Host { get { return new Uri($"{Configuration["MHost"]}"); } }
        public static string ClientId { get { return $"{Configuration["MClientId"]}"; } }
        public static string ClientSecret { get { return $"{Configuration["MClientSecret"]}"; } }
        public static string Username { get { return $"{Configuration["MUsername"]}"; } }
        public static string Password { get { return $"{Configuration["MPassword"]}"; } }
    }
}
