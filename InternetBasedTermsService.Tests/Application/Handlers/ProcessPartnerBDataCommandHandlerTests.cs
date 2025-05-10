using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using InternetBasedTermsService.Application.Commands;
using InternetBasedTermsService.Application.Handlers;

namespace InternetBasedTermsService.Tests.Application.Handlers;

 public class ProcessPartnerBDataCommandHandlerTests : IDisposable
    {
        private readonly Mock<ILogger<ProcessPartnerBDataCommandHandler>> _mockLogger;
        private readonly ProcessPartnerBDataCommandHandler _handler;
        private readonly string _testOutputDirectory;
        private string _expectedFilePath; // Dynamically set in tests

        // The handler uses a const for OutputFileName. To test this without modifying the handler,
        // we must assume it writes to the current working directory or a predictable path.
        // For tests, it's better to control this. If the handler could take an output path,
        // testing would be cleaner. Here, we'll assume the file is created relative to a test dir.
        private const string HandlerOutputFileName = "InstrumentNotification.xml";


        public ProcessPartnerBDataCommandHandlerTests()
        {
            _mockLogger = new Mock<ILogger<ProcessPartnerBDataCommandHandler>>();
            _handler = new ProcessPartnerBDataCommandHandler(_mockLogger.Object);

            // Create a unique temporary directory for each test run to avoid conflicts
            _testOutputDirectory = Path.Combine(Path.GetTempPath(), "PartnerBHandlerTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testOutputDirectory);
            _expectedFilePath = Path.Combine(_testOutputDirectory, HandlerOutputFileName);
        }

        // Helper to run the handler and temporarily change current directory for predictable output
        private async Task ExecuteHandlerInTestDirectory(ProcessPartnerBDataCommand command)
        {
            var originalDirectory = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(_testOutputDirectory);
                await _handler.Handle(command, CancellationToken.None);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDirectory);
            }
        }


        [Fact]
        public async Task Handle_EventType9097AndIsinExists_ShouldCreateXmlFileWithCorrectContent()
        {
            // Arrange
            var processingTime = new DateTime(2025, 05, 10, 14, 30, 00, DateTimeKind.Utc);
            var command = new ProcessPartnerBDataCommand(
                EventType: "9097",
                Isin: "ISINPARTNERB",
                ProcessingTimestamp: processingTime,
                CorrelationId: Guid.NewGuid()
            );

            // Act
            await ExecuteHandlerInTestDirectory(command);

            // Assert
            File.Exists(_expectedFilePath).Should().BeTrue($"File should be created at {_expectedFilePath}");
            var fileContent = await File.ReadAllTextAsync(_expectedFilePath);
            var xmlDoc = XDocument.Parse(fileContent);

            xmlDoc.Root.Should().NotBeNull();
            xmlDoc.Root.Name.LocalName.Should().Be("InstrumentNotification");
            xmlDoc.Root.Element("Timespan")?.Value.Should().Be(processingTime.ToString("o")); // ISO 8601 format
            xmlDoc.Root.Element("ISIN")?.Value.Should().Be(command.Isin);
        }

        [Fact]
        public async Task Handle_EventTypeNot9097_ShouldNotCreateFile()
        {
            // Arrange
            var command = new ProcessPartnerBDataCommand(
                EventType: "1234", // Not 9097
                Isin: "ISINNOFILE",
                ProcessingTimestamp: DateTime.UtcNow,
                CorrelationId: Guid.NewGuid()
            );

            // Act
            await ExecuteHandlerInTestDirectory(command);

            // Assert
            File.Exists(_expectedFilePath).Should().BeFalse();
        }

        [Fact]
        public async Task Handle_EventType9097ButIsinIsMissing_ShouldNotCreateFile()
        {
            // Arrange
            var command = new ProcessPartnerBDataCommand(
                EventType: "9097",
                Isin: "", // Empty ISIN
                ProcessingTimestamp: DateTime.UtcNow,
                CorrelationId: Guid.NewGuid()
            );

            // Act
            await ExecuteHandlerInTestDirectory(command);

            // Assert
            File.Exists(_expectedFilePath).Should().BeFalse();
            // Verify warning log
             _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Skipping file creation") && v.ToString().Contains("ISIN is missing")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        public void Dispose()
        {
            // Cleanup: Delete the temporary directory and its contents
            if (Directory.Exists(_testOutputDirectory))
            {
                try
                {
                    Directory.Delete(_testOutputDirectory, true);
                }
                catch (IOException ex)
                {
                    // Log or handle cleanup error, e.g., file lock
                    Console.WriteLine($"Error deleting test directory {_testOutputDirectory}: {ex.Message}");
                }
            }
        }
    }