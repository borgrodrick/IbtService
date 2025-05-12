using System.Reflection;
using InternetBasedTermsService.Application.Behaviours;
using InternetBasedTermsService.Application.Commands;
using InternetBasedTermsService.Application.Interfaces;
using InternetBasedTermsService.Infrastructure.Parsing;
using InternetBasedTermsService.Infrastructure.Persistence;
using InternetBasedTermsService.Infrastructure.Workers;
using MediatR;

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
                services.AddSingleton<IDatabaseLogger, DatabaseLoggerSimulator>();
                services.AddSingleton<XmlParser>();

                services.AddMediatR(cfg => {
                    // Scan for all MediatR handlers (IRequestHandler, INotificationHandler)
                    // Ensure all relevant handler assemblies are scanned.
                    // If handlers are in the same assembly as Program.cs:
                    cfg.RegisterServicesFromAssembly(typeof(LogEventCommand).Assembly);
                    // cfg.RegisterServicesFromAssembly(typeof(LoggingBehavior<,>).Assembly);


                    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                    // Or, specify the assembly more directly, e.g., if handlers are in a class library:
                    // cfg.RegisterServicesFromAssembly(typeof(LogEventCommandHandler).Assembly);

                    // Register the pipeline behavior
                    // This will apply to all IRequest handlers
                    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
                });

                services.AddHostedService<IngestionWorker>();
            });
}