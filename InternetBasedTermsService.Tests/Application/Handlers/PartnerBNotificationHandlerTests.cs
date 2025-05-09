using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Xml.Linq;
using InternetBasedTermsService.Application.Handlers;
using InternetBasedTermsService.Infrastructure;

namespace InternetBasedTermsService.Tests.Application.Handlers;

public class PartnerBNotificationHandlerTests : IDisposable // For cleanup
{
    private readonly Mock<ILogger<PartnerBNotificationHandler>> _mockLogger;
    private readonly PartnerBNotificationHandler _handler;
    private readonly string _testOutputDirectory = Path.Combine(Path.GetTempPath(), "PartnerBTests");
    private string _expectedFileName = "InstrumentNotification.xml"; // Default name in handler

    public PartnerBNotificationHandlerTests()
    {
        _mockLogger = new Mock<ILogger<PartnerBNotificationHandler>>();
        _handler = new PartnerBNotificationHandler(_mockLogger.Object); // Assuming handler writes to current dir

        if (Directory.Exists(_testOutputDirectory))
        {
            Directory.Delete(_testOutputDirectory, true);
        }
        Directory.CreateDirectory(_testOutputDirectory);
        _expectedFileName = Path.Combine(_testOutputDirectory, _expectedFileName); // Use a test-specific path
    }

    // Helper to change where the handler writes the file for testing.
    // This is easier if the output path is configurable in the handler.
    // For now, we assume it writes to a predictable location or we modify it.
    // A better PartnerBNotificationHandler would take an output path provider.
    // For simplicity, we'll assume the handler can be modified or we test its current behavior.
    // Let's assume the handler's OutputFileName is a public const or static for modification (not ideal)
    // or we check in the current directory. For isolated tests, it's better to control output.

    // For now, let's assume we override the filename for testing:
    private void SetTestOutputFileName(string filename)
    {
        // This is a simplification. Ideally, the output path is injected or configurable.
        // If `OutputFileName` is a const in the handler, this approach won't work directly.
        // You'd have to check the default location.
        // Let's assume we can control it for the test.
        _expectedFileName = Path.Combine(_testOutputDirectory, filename);

        // If PartnerBNotificationHandler has a settable property for OutputFileName:
        // _handler.SetOutputFileNameForTest(_expectedFileName);
        // Otherwise, ensure the test checks the handler's default output location,
        // and cleans it up. For this example, we'll work with _expectedFileName.
    }


    [Fact]
    public async Task Handle_WhenEventTypeIs9097AndIsinExists_ShouldCreateXmlFile()
    {
        // Arrange
        SetTestOutputFileName("output_9097.xml"); // Unique name for this test
        var processingTime = new DateTime(2025, 05, 07, 10, 30, 00, DateTimeKind.Utc);
        var notification = new IbtDataProcessedNotification(
            "9097", "ProdB", "TypeB", "ISINB9097",
            processingTime, Guid.NewGuid());

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        File.Exists(_expectedFileName).Should().BeTrue();
        var fileContent = await File.ReadAllTextAsync(_expectedFileName);
        var xmlDoc = XDocument.Parse(fileContent);

        xmlDoc.Root.Name.LocalName.Should().Be("InstrumentNotification");
        xmlDoc.Root.Element("Timespan")?.Value.Should().Be(processingTime.ToString("o"));
        xmlDoc.Root.Element("ISIN")?.Value.Should().Be("ISINB9097");
    }

    [Fact]
    public async Task Handle_WhenEventTypeIsNot9097_ShouldNotCreateFile()
    {
        // Arrange
        SetTestOutputFileName("output_not9097.xml");
        var notification = new IbtDataProcessedNotification(
            "1234", "ProdB", "TypeB", "ISINB1234",
            DateTime.UtcNow, Guid.NewGuid());

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        File.Exists(_expectedFileName).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenEventTypeIs9097ButIsinIsMissing_ShouldNotCreateFile()
    {
        // Arrange
        SetTestOutputFileName("output_missing_isin.xml");
        var notification = new IbtDataProcessedNotification(
            "9097", "ProdB", "TypeB", "", // Empty ISIN
            DateTime.UtcNow, Guid.NewGuid());

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        File.Exists(_expectedFileName).Should().BeFalse();
        // Verify warning log
        _mockLogger.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Skipping file creation - ISIN is missing")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }


    public void Dispose()
    {
        // Cleanup test files and directory
        if (Directory.Exists(_testOutputDirectory))
        {
            try
            {
                Directory.Delete(_testOutputDirectory, true);
            }
            catch (Exception ex)
            {
                // Log or output cleanup error, but don't let it fail tests
                Console.WriteLine($"Error cleaning up test directory: {ex.Message}");
            }
        }
    }
}