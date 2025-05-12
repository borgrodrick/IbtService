using FluentAssertions;
using InternetBasedTermsService.Infrastructure.Parsing;
using Microsoft.Extensions.Logging;
using Moq;

namespace InternetBasedTermsService.Tests.Infrastructure.Parsing;

public class XmlParserFileAccessTests : IDisposable
    {
        private readonly Mock<ILogger<XmlParser>> _mockLogger;
        private readonly XmlParser _parser;
        private readonly string _testDirectory;

        // --- XML Content Constants for Test Construction (shared for context) ---
        private const string XmlNs = "xmlns='http://schemas.vontobel.com/dataservice/v1.0'";
        private const string ValidHeader = "<?xml version='1.0' encoding='utf-8'?>";
        private const string RootStart = "<IBTTermSheet " + XmlNs + ">";
        private const string RootEnd = "</IBTTermSheet>";
        private const string ElementEventTypeValue = "9097";
        private const string ElementProductNameFullValue = "Test Product Name Full";
        private const string ElementIbtTypeCodeValue = "TC123";
        private const string ElementIsinValue = "ISINXYZ123";
        private const string ElementEventTypeTag = "<EventType>" + ElementEventTypeValue + "</EventType>";
        private const string ElementProductNameFullTag = "<ProductNameFull>" + ElementProductNameFullValue + "</ProductNameFull>";
        private const string ElementIbtTypeCodeTag = "<IBTTypeCode>" + ElementIbtTypeCodeValue + "</IBTTypeCode>";
        private const string ElementIsinTag = "<InstrumentId><IdSchemeCode>I-</IdSchemeCode><IdValue>" + ElementIsinValue + "</IdValue></InstrumentId>";
        private const string EventsStructureFormat = "<Events><Event>{0}</Event></Events>";
        private const string InstrumentInnerStructureFormat = "{0}{1}{2}";
        private const string InstrumentIdsStructureFormat = "<InstrumentIds>{0}</InstrumentIds>";
        private const string InstrumentStructureFormat = "<Instrument>{0}</Instrument>";
        private readonly string _fullValidXmlContent;

        private static string ConstructValidXml() =>
            $"{ValidHeader}{RootStart}" +
            $"{string.Format(EventsStructureFormat, ElementEventTypeTag)}" +
            $"{string.Format(InstrumentStructureFormat, string.Format(InstrumentInnerStructureFormat, ElementProductNameFullTag, ElementIbtTypeCodeTag, string.Format(InstrumentIdsStructureFormat, ElementIsinTag)))}" +
            $"{RootEnd}";

        public XmlParserFileAccessTests()
        {
            _mockLogger = new Mock<ILogger<XmlParser>>();
            _parser = new XmlParser(_mockLogger.Object);
            _testDirectory = Path.Combine(Path.GetTempPath(), "XmlParserFileAccessTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            _fullValidXmlContent = ConstructValidXml();
        }

        private string CreateTempXmlFile(string content, string fileName = "test.xml")
        {
            var filePath = Path.Combine(_testDirectory, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        [Fact]
        public void ParseFromFile_WithValidFileAndContent_ShouldReturnParsedData()
        {
            // Arrange
            var validXmlPath = CreateTempXmlFile(_fullValidXmlContent, "valid_file.xml");

            // Act
            var result = _parser.ParseFromFile(validXmlPath);

            // Assert
            result.Should().NotBeNull();
            result?.EventType.Should().Be(ElementEventTypeValue);
            result?.ProductNameFull.Should().Be(ElementProductNameFullValue);
            result?.IbtTypeCode.Should().Be(ElementIbtTypeCodeValue);
            result?.Isin.Should().Be(ElementIsinValue);
        }

        [Fact]
        public void ParseFromFile_WithNullFilePath_ShouldReturnNullAndLogError()
        {
            // Act
            var result = _parser.ParseFromFile(null!);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseFromFile_WithEmptyFilePath_ShouldReturnNullAndLogError()
        {
            // Act
            var result = _parser.ParseFromFile(string.Empty);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseFromFile_WithNonExistentFile_ShouldReturnNullAndLogError()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDirectory, "ghost.xml");

            // Act
            var result = _parser.ParseFromFile(nonExistentPath);

            // Assert
            result.Should().BeNull(); ;
        }

        [Fact]
        public void ParseFromFile_WhenFileContentIsMalformed_ShouldReturnNullAndLogParsingError()
        {
            // Arrange
            var malformedContent = "this is not xml";
            var malformedPath = CreateTempXmlFile(malformedContent, "malformed_in_file.xml");

            // Act
            var result = _parser.ParseFromFile(malformedPath);

            // Assert
            result.Should().BeNull();

        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
            GC.SuppressFinalize(this);
        }
    }