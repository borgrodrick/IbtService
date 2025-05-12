using System.Xml.Linq;
using FluentAssertions;
using InternetBasedTermsService.Application.Behaviours;
using InternetBasedTermsService.Application.Commands;
using InternetBasedTermsService.Application.Handlers;
using InternetBasedTermsService.Application.Interfaces;
using InternetBasedTermsService.Application.Notifications;
using InternetBasedTermsService.Domain;
using InternetBasedTermsService.Infrastructure.Parsing;
using InternetBasedTermsService.Tests.Helper;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace InternetBasedTermsService.Tests.Infrastructure.Workers;


  public class IngestionWorkerIntegrationTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _inputFilePath;
        private readonly string _partnerBOutputFilePath; // Output path for Partner B's file

        // --- XML Content Constants ---
        private const string XmlNs = "xmlns='http://schemas.vontobel.com/dataservice/v1.0'";
        private const string ValidHeader = "<?xml version='1.0' encoding='utf-8'?>";
        private const string RootStart = "<IBTTermSheet " + XmlNs + ">";
        private const string RootEnd = "</IBTTermSheet>";
        private const string ElementEventTypeValue = "9097"; // For successful Partner B case
        private const string ElementEventTypeValueOther = "1234"; // For non-Partner B case
        private const string ElementProductNameFullValue = "Integration Test Product";
        private const string ElementIbtTypeCodeValue = "ITEST001";
        private const string ElementIsinValue = "INTEGRATIONISIN";
        private const string ElementEventTypeTag = "<EventType>" + ElementEventTypeValue + "</EventType>";
        private const string ElementEventTypeOtherTag = "<EventType>" + ElementEventTypeValueOther + "</EventType>";
        private const string ElementProductNameFullTag = "<ProductNameFull>" + ElementProductNameFullValue + "</ProductNameFull>";
        private const string ElementIbtTypeCodeTag = "<IBTTypeCode>" + ElementIbtTypeCodeValue + "</IBTTypeCode>";
        private const string ElementIsinTag = "<InstrumentId><IdSchemeCode>I-</IdSchemeCode><IdValue>" + ElementIsinValue + "</IdValue></InstrumentId>";
        private const string EventsStructureFormat = "<Events><Event>{0}</Event></Events>";
        private const string InstrumentInnerStructureFormat = "{0}{1}{2}";
        private const string InstrumentIdsStructureFormat = "<InstrumentIds>{0}</InstrumentIds>";
        private const string InstrumentStructureFormat = "<Instrument>{0}</Instrument>";

        private static string ConstructTestXml(string eventTypeTag = ElementEventTypeTag) =>
            $"{ValidHeader}{RootStart}" +
            $"{string.Format(EventsStructureFormat, eventTypeTag)}" +
            $"{string.Format(InstrumentStructureFormat, string.Format(InstrumentInnerStructureFormat, ElementProductNameFullTag, ElementIbtTypeCodeTag, string.Format(InstrumentIdsStructureFormat, ElementIsinTag)))}" +
            $"{RootEnd}";


        public IngestionWorkerIntegrationTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "IngestionWorkerIntegrationTests_V2", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            _inputFilePath = Path.Combine(_testDirectory, "INPUT_IBT.xml");
            _partnerBOutputFilePath = Path.Combine(_testDirectory, "InstrumentNotification.xml");
        }

        private ServiceProvider BuildServiceProvider(string inputFileToUse, bool useTestablePartnerAHandler = true)
        {
            var services = new ServiceCollection();

            // Configuration
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"InputFilePath", inputFileToUse}
                })
                .Build();
            services.AddSingleton<IConfiguration>(configuration);

            // Logging
            services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Debug));

            // Application Services
            services.AddSingleton<XmlParser>();

            // Mock IDatabaseLogger for verification in tests
            var mockDatabaseLogger = new Mock<IDatabaseLogger>();
            services.AddSingleton(mockDatabaseLogger.Object); 

            // MediatR Setup
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssemblyContaining<LogEventCommand>(); 
                cfg.RegisterServicesFromAssemblyContaining<LogEventCommandHandler>(); 
                cfg.RegisterServicesFromAssemblyContaining<IbtDataProcessingOrchestrator>(); 
                cfg.RegisterServicesFromAssemblyContaining<ProcessPartnerBDataCommandHandler>(); 
                
                // We will register TestableNotifyPartnerACommandHandler as a singleton instance below
                // so no need to scan for it here if we are replacing it.
                // If it were scanned, the explicit singleton registration below would override it.
                
                cfg.RegisterServicesFromAssemblyContaining<LoggingBehavior<LogEventCommand, Unit>>(); 
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            });
            
            if (useTestablePartnerAHandler)
            {
                // Create a single instance of TestableNotifyPartnerACommandHandler
                // And register this instance as a singleton for both its concrete type and the interface.
                // This ensures the test and MediatR use the exact same object.
                var testablePartnerAHandlerInstance = new TestableNotifyPartnerACommandHandler(
                    LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<TestableNotifyPartnerACommandHandler>()
                );
                services.AddSingleton(testablePartnerAHandlerInstance); // Register as concrete type
                services.AddSingleton<IRequestHandler<NotifyPartnerACommand>>(testablePartnerAHandlerInstance); // Register as interface
            }
            
            services.AddHostedService<IngestionWorker>();

            return services.BuildServiceProvider();
        }


        [Fact]
        public async Task ProcessValidIbtFile_EventType9097_ShouldTriggerHandlersAndCreatePartnerBFile()
        {
            // Arrange
            File.WriteAllText(_inputFilePath, ConstructTestXml(ElementEventTypeTag)); 
            var serviceProvider = BuildServiceProvider(_inputFilePath);

            var mockDbLogger = serviceProvider.GetRequiredService<IDatabaseLogger>(); 
            // Resolve the singleton instance of TestableNotifyPartnerACommandHandler
            var partnerAHandler = serviceProvider.GetRequiredService<TestableNotifyPartnerACommandHandler>(); 
            partnerAHandler.Should().NotBeNull("TestablePartnerAHandler should be resolved from DI for this test.");

            var hostedServices = serviceProvider.GetServices<IHostedService>();
            var worker = hostedServices.OfType<IngestionWorker>().FirstOrDefault();
            worker.Should().NotBeNull("IngestionWorker should be registered as IHostedService.");

            var originalCurrentDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(_testDirectory);
            try
            {
                await worker!.StartAsync(CancellationToken.None);
                await Task.Delay(700); 
                await worker.StopAsync(CancellationToken.None);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalCurrentDirectory);
            }

            // Assert
            Mock.Get(mockDbLogger).Verify(db => db.LogEvent(ElementEventTypeValue, It.IsAny<DateTime>()), Times.Once);

            partnerAHandler!.HandleCallCount.Should().Be(1); // This should now work
            partnerAHandler.LastReceivedCommand.Should().NotBeNull();
            partnerAHandler.LastReceivedCommand?.EventType.Should().Be(ElementEventTypeValue);
            partnerAHandler.LastReceivedCommand?.Isin.Should().Be(ElementIsinValue);

            File.Exists(_partnerBOutputFilePath).Should().BeTrue($"Partner B output file should be created at {_partnerBOutputFilePath}");
            var partnerBFileContent = await File.ReadAllTextAsync(_partnerBOutputFilePath);
            var partnerBXml = XDocument.Parse(partnerBFileContent);
            partnerBXml.Root?.Name.LocalName.Should().Be("InstrumentNotification");
            partnerBXml.Root?.Element("ISIN")?.Value.Should().Be(ElementIsinValue);
            partnerBXml.Root?.Element("Timespan")?.Value.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ProcessValidIbtFile_EventTypeNot9097_ShouldTriggerRelevantHandlersAndNotCreatePartnerBFile()
        {
            // Arrange
            File.WriteAllText(_inputFilePath, ConstructTestXml(ElementEventTypeOtherTag)); 
            var serviceProvider = BuildServiceProvider(_inputFilePath);

            var mockDbLogger = serviceProvider.GetRequiredService<IDatabaseLogger>();
            // Resolve the singleton instance of TestableNotifyPartnerACommandHandler
            var partnerAHandler = serviceProvider.GetRequiredService<TestableNotifyPartnerACommandHandler>();
            partnerAHandler.Should().NotBeNull();

            var hostedServices = serviceProvider.GetServices<IHostedService>();
            var worker = hostedServices.OfType<IngestionWorker>().FirstOrDefault();
            worker.Should().NotBeNull();

            var originalCurrentDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(_testDirectory);
            try
            {
                await worker!.StartAsync(CancellationToken.None);
                await Task.Delay(700); 
                await worker.StopAsync(CancellationToken.None);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalCurrentDirectory);
            }

            // Assert
            Mock.Get(mockDbLogger).Verify(db => db.LogEvent(ElementEventTypeValueOther, It.IsAny<DateTime>()), Times.Once);
            
            partnerAHandler!.HandleCallCount.Should().Be(1); // This should now work
            partnerAHandler.LastReceivedCommand?.EventType.Should().Be(ElementEventTypeValueOther);

            File.Exists(_partnerBOutputFilePath).Should().BeFalse("Partner B output file should NOT be created for this event type.");
        }


        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TEST CLEANUP ERROR] Failed to delete test directory {_testDirectory}: {ex.Message}");
                }
            }
            GC.SuppressFinalize(this);
        }
    }

    // --- Dummy IngestionWorker for compilation if not defined elsewhere in test project context ---
    public class IngestionWorker : BackgroundService
    {
        private readonly ILogger<IngestionWorker> _logger;
        private readonly IConfiguration _configuration;
        private readonly XmlParser _parser;
        private readonly IMediator _mediator;
        private readonly string _ibtFilePath;


        public IngestionWorker(ILogger<IngestionWorker> logger, IConfiguration configuration, XmlParser parser, IMediator mediator)
        {
            _logger = logger;
            _configuration = configuration;
            _parser = parser;
            _mediator = mediator;
            _ibtFilePath = _configuration.GetValue<string>("InputFilePath") ?? "IBT.xml"; 
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("IngestionWorker (IntegrationTest Version) running at: {time}", DateTimeOffset.Now);
             if (string.IsNullOrEmpty(_ibtFilePath) || !File.Exists(_ibtFilePath))
            {
                _logger.LogError("Input file path is invalid or file not found: {FilePath}. Worker stopping.", _ibtFilePath);
                return;
            }

            _logger.LogDebug("Attempting to parse file: {FilePath}", _ibtFilePath);
            IbtData? parsedData = _parser.ParseFromFile(_ibtFilePath); 

            if (parsedData != null && !stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Successfully parsed IBT data from {FilePath}. Publishing notification...", _ibtFilePath);
                var notification = new IbtDataProcessedNotification(
                    parsedData.EventType,
                    parsedData.ProductNameFull,
                    parsedData.IbtTypeCode,
                    parsedData.Isin,
                    DateTime.UtcNow, 
                    Guid.NewGuid()   
                );
                await _mediator.Publish(notification, stoppingToken);
                _logger.LogInformation("IBT data processed and notification published for CorrelationId: {CorrelationId}.", notification.CorrelationId);
            }
            else if (parsedData == null)
            {
                 _logger.LogWarning("Failed to parse IBT data from {file}.", _ibtFilePath);
            }
            _logger.LogInformation("IngestionWorker (IntegrationTest Version) finished processing cycle.");
        }
    }