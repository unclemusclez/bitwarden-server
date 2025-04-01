using AspNetCoreRateLimit;
using Bit.Core.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Bit.Identity;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args)
            .Build()
            .Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) => // Changed from ConfigureCustomAppConfiguration
            {
                Console.WriteLine($"Environment: {context.HostingEnvironment.EnvironmentName}");
                Console.WriteLine("Initial Config Sources:");
                LogConfigSources(config);

                config.SetBasePath(Directory.GetCurrentDirectory());
                Console.WriteLine("After SetBasePath:");
                LogConfigValues(config.Build());

                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                Console.WriteLine("After adding appsettings.json:");
                LogConfigSources(config);
                LogConfigValues(config.Build());

                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                Console.WriteLine($"After adding appsettings.{context.HostingEnvironment.EnvironmentName}.json:");
                LogConfigSources(config);
                LogConfigValues(config.Build());

                if (context.HostingEnvironment.IsDevelopment())
                {
                    config.AddUserSecrets<Program>();
                    Console.WriteLine("After adding user secrets:");
                    LogConfigSources(config);
                    LogConfigValues(config.Build());
                }

                config.AddEnvironmentVariables();
                Console.WriteLine("After adding environment variables:");
                LogConfigSources(config);
                LogConfigValues(config.Build());

                config.AddCommandLine(args);
                Console.WriteLine("After adding command-line args:");
                LogConfigSources(config);
                LogConfigValues(config.Build());
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.ConfigureLogging((hostingContext, logging) =>
                    logging.AddSerilog(hostingContext, (e, globalSettings) =>
                    {
                        var context = e.Properties["SourceContext"].ToString();
                        if (context.Contains(typeof(IpRateLimitMiddleware).FullName))
                        {
                            return e.Level >= globalSettings.MinLogLevel.IdentitySettings.IpRateLimit;
                        }
                        if (context.Contains("Duende.IdentityServer.Validation.TokenValidator") ||
                            context.Contains("Duende.IdentityServer.Validation.TokenRequestValidator"))
                        {
                            return e.Level >= globalSettings.MinLogLevel.IdentitySettings.IdentityToken;
                        }
                        return e.Level >= globalSettings.MinLogLevel.IdentitySettings.Default;
                    }));
            });
    }

    private static void LogConfigSources(IConfigurationBuilder config)
    {
        foreach (var source in config.Sources)
        {
            Console.WriteLine($"Config Source: {source}");
        }
    }

    private static void LogConfigValues(IConfiguration config)
    {
        Console.WriteLine($"DatabaseProvider: '{config["globalSettings:DatabaseProvider"]}'");
        Console.WriteLine($"Sqlite Connection: '{config["globalSettings:Sqlite:connectionString"]}'");
        Console.WriteLine($"SqlServer Connection: '{config["globalSettings:sqlServer:connectionString"]}'");
    }
}
