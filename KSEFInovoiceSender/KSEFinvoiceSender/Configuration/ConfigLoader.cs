using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSEFinvoiceSender.Configuration;
public static class ConfigLoader
{
    private static readonly Lazy<IConfigurationRoot> _configuration = new Lazy<IConfigurationRoot>(() =>
    {
        try
        {
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL: Could not build configuration root. {ex.Message}");
            throw;
        }
    });

    public static T LoadSection<T>(string sectionName) where T : new()
    {
        try
        {
            var section = _configuration.Value.GetSection(sectionName);

            if (!section.Exists())
            {
                throw new InvalidOperationException($"Configuration section '{sectionName}' is missing.");
            }

            var configInstance = new T();
            section.Bind(configInstance);

            return configInstance;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Failed to bind configuration section '{sectionName}'. {ex.Message}");
            throw;
        }
    }
}