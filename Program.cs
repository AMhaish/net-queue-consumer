using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Azure.ServiceBus;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace queue_consumer
{
    class Program
    {
        static async Task MainAsync(string[] args)
        {
            var hostBuilder = new HostBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", false);
                config.AddEnvironmentVariables();
                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.ClearProviders();
                var options = new ConsoleLoggerOptions(logging.Services.BuildServiceProvider().GetRequiredService<IConfiguration>());
                logging.AddProvider(
                    new ConsoleLoggerProvider(options)
                );
            })
             // Add configuration, logging, ...
            .ConfigureServices((hostContext, services) =>
            {
                services.AddOptions();
                services.AddHostedService<HostedService>();
                
                // Add your services with dependency injection.
            });
            await hostBuilder.RunConsoleAsync();
        }

        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }
    }
}

