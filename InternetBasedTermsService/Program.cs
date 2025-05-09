using InternetBasedTermsService.Application.Handlers;
using InternetBasedTermsService.Application.Parsing;
using InternetBasedTermsService.Infrastructure;
using InternetBasedTermsService.Infrastructure.Workers;

namespace InternetBasedTermsService;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // Register application services needed by handlers or worker
                services.AddSingleton<DatabaseLoggerSimulator>();
                services.AddSingleton<XmlParser>(); // For IngestionWorker and potentially direct use

                // Register MediatR and scan for Handlers (INotificationHandler)
                // Ensure the assembly containing your notification handlers is specified.
                services.AddMediatR(cfg =>
                    cfg.RegisterServicesFromAssembly(typeof(DbLoggingNotificationHandler).Assembly));
                // Or use Assembly.GetExecutingAssembly() if handlers are in the main project.

                // Register the IngestionWorker as a Hosted Service
                services.AddHostedService<IngestionWorker>();

            });
}