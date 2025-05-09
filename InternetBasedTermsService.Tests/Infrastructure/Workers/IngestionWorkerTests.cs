using InternetBasedTermsService.Application.Parsing;
using InternetBasedTermsService.Infrastructure;
using InternetBasedTermsService.Infrastructure.Workers;

using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using System;
using InternetBasedTermsService.Domain;

// Assuming IngestionWorker is in the main project's root or a specific namespace
// For example: using IbtProcessingApp.Infrastructure.Workers;

namespace InternetBasedTermsService.Tests.Infrastructure.Workers;

public class IngestionWorkerTests
{
    private readonly Mock<XmlParser> _mockXmlParser;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<IngestionWorker>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly IngestionWorker _worker;

    public IngestionWorkerTests()
    {
        _mockLogger = new Mock<ILogger<IngestionWorker>>();
        // We need to mock XmlParser because its Parse method takes ILogger,
        // or pass a real logger if XmlParser itself is not the SUT here but its interaction.
        // For unit testing IngestionWorker, mocking XmlParser is cleaner.
        _mockXmlParser = new Mock<XmlParser>(Mock.Of<ILogger<XmlParser>>());
        _mockMediator = new Mock<IMediator>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Setup default configuration for InputFilePath
        _mockConfiguration.Setup(c => c.GetValue<string>("InputFilePath")).Returns("test.xml");

        _worker = new IngestionWorker(
            _mockLogger.Object,
            _mockXmlParser.Object,
            _mockMediator.Object,
            _mockConfiguration.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenParserSucceeds_ShouldPublishNotification()
    {
        // Arrange
        var filePath = "test.xml"; // Value from mock config
        var parsedData = new IbtData
        {
            EventType = "123",
            ProductNameFull = "Test Prod",
            IbtTypeCode = "ABC",
            Isin = "ISINXYZ"
        };
        _mockXmlParser.Setup(p => p.Parse(filePath)).Returns(parsedData);

        // Mock File.Exists to return true for the configured path
        // This is a bit tricky as File.Exists is static.
        // For more robust testing of file interactions, you might wrap file system operations
        // in an interface and mock that interface. For this exercise, we'll assume
        // the file exists check passes if parser is going to return data.
        // A simpler way is to ensure the worker's logic handles a null from parser.

        IbtDataProcessedNotification? publishedNotification = null;
        _mockMediator.Setup(m => m.Publish(It.IsAny<IbtDataProcessedNotification>(), It.IsAny<CancellationToken>()))
            .Callback<INotification, CancellationToken>((notification, ct) => publishedNotification = notification as IbtDataProcessedNotification)
            .Returns(Task.CompletedTask);

        // Act
        // We need to pass a CancellationToken. A real one or CancellationToken.None.
        // Since ExecuteAsync in BackgroundService is protected, we'd typically test the public method that calls it,
        // or make ExecuteAsync internal and use InternalsVisibleTo for testing.
        // For simplicity, we'll assume we can call a method that encapsulates the core logic.
        // If directly testing ExecuteAsync, you might need to call StartAsync then StopAsync.
        // For this example, we'll assume we can simulate the core execution part.

        // We will simulate the file existing by having the parser return data.
        // The worker's own File.Exists check is hard to mock directly.
        // A better IngestionWorker would take an IFileSystem abstraction.

        // For this test, let's simplify and assume the file check is implicitly handled.
        // The worker has a File.Exists check. To make this test pass without a real file,
        // we must ensure the parser is the gatekeeper.
        // If the file doesn't exist, parser would be called with a path that leads to its own failure (return null).

        // Re-arranging to test the flow IF parsing succeeds
        _mockXmlParser.Setup(p => p.Parse(It.IsAny<string>())).Returns(parsedData);


        await _worker.StartAsync(CancellationToken.None); // Start the worker
        // Give it a brief moment to execute, or use a ManualResetEvent if more complex timing is needed.
        await Task.Delay(100); // Small delay to allow ExecuteAsync to run
        await _worker.StopAsync(CancellationToken.None); // Stop it

        // Assert
        _mockMediator.Verify(m => m.Publish(It.IsAny<IbtDataProcessedNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        publishedNotification.Should().NotBeNull();
        publishedNotification.EventType.Should().Be(parsedData.EventType);
        publishedNotification.Isin.Should().Be(parsedData.Isin);
    }

    [Fact]
    public async Task ExecuteAsync_WhenParserFails_ShouldNotPublishNotification()
    {
        // Arrange
        _mockXmlParser.Setup(p => p.Parse(It.IsAny<string>())).Returns((IbtData?)null);

        // Act
        await _worker.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await _worker.StopAsync(CancellationToken.None);

        // Assert
        _mockMediator.Verify(m => m.Publish(It.IsAny<IbtDataProcessedNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

     [Fact]
    public async Task ExecuteAsync_WhenInputFileNotFoundConfigured_WorkerLogsErrorAndDoesNotPublish()
    {
        // Arrange
        // Configure a file path that definitely won't exist for the parser setup
        _mockConfiguration.Setup(c => c.GetValue<string>("InputFilePath")).Returns("non_existent_for_worker.xml");
        // Parser should not even be called if worker's File.Exists fails.
        // To test THIS specifically, the File.Exists needs to be mockable.
        // Given the current IngestionWorker code, if File.Exists returns false, parser is not called.

        // We are testing the worker's own File.Exists check here.
        var workerWithNonExistentFile = new IngestionWorker(
            _mockLogger.Object,
            _mockXmlParser.Object, // Parser won't be called
            _mockMediator.Object,  // Mediator won't be called
            _mockConfiguration.Object);


        // Act
        await workerWithNonExistentFile.StartAsync(CancellationToken.None);
        await Task.Delay(100); // Allow ExecuteAsync to run its course
        await workerWithNonExistentFile.StopAsync(CancellationToken.None);

        // Assert
        _mockXmlParser.Verify(p => p.Parse(It.IsAny<string>()), Times.Never); // Parser should not be called
        _mockMediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        // Optionally, verify logger was called with an error about file not found.
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Input file not found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}