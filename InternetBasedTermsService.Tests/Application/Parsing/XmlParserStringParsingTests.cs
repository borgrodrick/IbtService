using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using InternetBasedTermsService.Application.Parsing;

namespace InternetBasedTermsService.Tests.Application.Parsing;

public class XmlParserStringParsingTests
{
    
    private readonly Mock<ILogger<XmlParser>> _mockLogger;
    private readonly XmlParser _parser;

    // --- XML Content Constants for Test Construction ---
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
    private const string ElementIsinWrongSchemeTag = "<InstrumentId><IdSchemeCode>CH</IdSchemeCode><IdValue>CHWRONGISIN</IdValue></InstrumentId>";

    private const string EventsStructureFormat = "<Events><Event>{0}</Event></Events>";
    private const string InstrumentInnerStructureFormat = "{0}{1}{2}";
    private const string InstrumentIdsStructureFormat = "<InstrumentIds>{0}</InstrumentIds>";
    private const string InstrumentStructureFormat = "<Instrument>{0}</Instrument>";

    private readonly string _fullValidXmlContent;

    private static string ConstructXml(
        string? eventTypeElement = ElementEventTypeTag,
        string? productNameFullElement = ElementProductNameFullTag,
        string? ibtTypeCodeElement = ElementIbtTypeCodeTag,
        string? isinElement = ElementIsinTag,
        bool includeEventsNode = true,
        bool includeInstrumentNode = true,
        bool includeInstrumentIdsNode = true)
    {
        string eventsXml = includeEventsNode ? string.Format(EventsStructureFormat, eventTypeElement ?? "") : "";
        string instrumentIdsContent = includeInstrumentIdsNode ? string.Format(InstrumentIdsStructureFormat, isinElement ?? "") : "";
        string instrumentInnerContent = string.Format(InstrumentInnerStructureFormat,
                                                      productNameFullElement ?? "",
                                                      ibtTypeCodeElement ?? "",
                                                      instrumentIdsContent);
        string instrumentXml = includeInstrumentNode ? string.Format(InstrumentStructureFormat, instrumentInnerContent) : "";
        return $"{ValidHeader}{RootStart}{eventsXml}{instrumentXml}{RootEnd}";
    }

    public XmlParserStringParsingTests()
    {
        _mockLogger = new Mock<ILogger<XmlParser>>();
        _parser = new XmlParser(_mockLogger.Object);
        _fullValidXmlContent = ConstructXml();
    }

    [Fact]
    public void ParseXmlString_WithValidXmlContent_ShouldReturnParsedData()
    {
        // Act
        var result = _parser.ParseXmlString(_fullValidXmlContent);

        // Assert
        result.Should().NotBeNull();
        result?.EventType.Should().Be(ElementEventTypeValue);
        result?.ProductNameFull.Should().Be(ElementProductNameFullValue);
        result?.IbtTypeCode.Should().Be(ElementIbtTypeCodeValue);
        result?.Isin.Should().Be(ElementIsinValue);
        _mockLogger.Verify(logger => logger.Log(
            LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
        _mockLogger.Verify(logger => logger.Log(
            LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void ParseXmlString_WithNullContent_ShouldReturnNullAndLogError()
    {
        // Act
        var result = _parser.ParseXmlString(null);

        // Assert
        result.Should().BeNull();
        _mockLogger.Verify(logger => logger.Log(
            LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true),
            null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void ParseXmlString_WithEmptyContent_ShouldReturnNullAndLogError()
    {
        // Act
        var result = _parser.ParseXmlString(string.Empty);

        // Assert
        result.Should().BeNull();
        _mockLogger.Verify(logger => logger.Log(
            LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true),
            null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void ParseXmlString_WithMalformedXml_ShouldReturnNullAndLogError()
    {
        // Arrange
        var malformedXml = "<?xml version='1.0'?><Root><Unclosed></Root>";

        // Act
        var result = _parser.ParseXmlString(malformedXml);

        // Assert
        result.Should().BeNull();
        _mockLogger.Verify(logger => logger.Log(
            LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<System.Xml.XmlException>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void ParseXmlString_WithMissingRootElement_ShouldReturnNullAndLogError()
    {
        // Arrange
        var noRootXml = "<?xml version='1.0' encoding='utf-8'?>"; // Just the XML declaration

        // Act
        var result = _parser.ParseXmlString(noRootXml);

        // Assert
        result.Should().BeNull();
        // This scenario will cause XDocument.Parse to throw an XmlException
        _mockLogger.Verify(logger => logger.Log(
            LogLevel.Error, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true), // Check that an error was logged
            It.IsAny<System.Xml.XmlException>(), // Expect an XmlException
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void ParseXmlString_CorrectlyParses_EventType()
    {
        var xml = ConstructXml(eventTypeElement: ElementEventTypeTag);
        var result = _parser.ParseXmlString(xml);
        result.Should().NotBeNull();
        result?.EventType.Should().Be(ElementEventTypeValue);
    }

    [Fact]
    public void ParseXmlString_MissingEventTypeElement_ShouldReturnNullAndLogWarning()
    {
        var xml = ConstructXml(eventTypeElement: "", includeEventsNode: true);
        var result = _parser.ParseXmlString(xml);
        result.Should().BeNull();
        _mockLogger.Verify(logger => logger.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
    
    [Fact]
    public void ParseXmlString_EmptyEventTypeValue_ShouldReturnNullAndLogWarning()
    {
        var xml = ConstructXml(eventTypeElement: "<EventType></EventType>");
        var result = _parser.ParseXmlString(xml);
        result.Should().BeNull();
        _mockLogger.Verify(logger => logger.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void ParseXmlString_CorrectlyParses_ProductNameFull()
    {
        var xml = ConstructXml(productNameFullElement: ElementProductNameFullTag);
        var result = _parser.ParseXmlString(xml);
        result.Should().NotBeNull();
        result?.ProductNameFull.Should().Be(ElementProductNameFullValue);
    }

    [Fact]
    public void ParseXmlString_MissingProductNameFullElement_ShouldReturnNullAndLogWarning()
    {
        var xml = ConstructXml(productNameFullElement: null);
        var result = _parser.ParseXmlString(xml);
        result.Should().BeNull();
        _mockLogger.Verify(logger => logger.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void ParseXmlString_CorrectlyParses_IbtTypeCode()
    {
        var xml = ConstructXml(ibtTypeCodeElement: ElementIbtTypeCodeTag);
        var result = _parser.ParseXmlString(xml);
        result.Should().NotBeNull();
        result?.IbtTypeCode.Should().Be(ElementIbtTypeCodeValue);
    }

    [Fact]
    public void ParseXmlString_MissingIbtTypeCodeElement_ShouldReturnNullAndLogWarning()
    {
        var xml = ConstructXml(ibtTypeCodeElement: null);
        var result = _parser.ParseXmlString(xml);
        result.Should().BeNull();
        _mockLogger.Verify(logger => logger.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void ParseXmlString_CorrectlyParses_Isin()
    {
        var xml = ConstructXml(isinElement: ElementIsinTag);
        var result = _parser.ParseXmlString(xml);
        result.Should().NotBeNull();
        result?.Isin.Should().Be(ElementIsinValue);
    }

    [Fact]
    public void ParseXmlString_MissingIsinElementStructure_ShouldReturnNullAndLogWarning()
    {
        var xml = ConstructXml(isinElement: null, includeInstrumentIdsNode: false);
        var result = _parser.ParseXmlString(xml);
        result.Should().BeNull();
        _mockLogger.Verify(logger => logger.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
    
    [Fact]
    public void ParseXmlString_IsinPresentButWrongScheme_ShouldReturnNullAndLogWarning()
    {
        var xml = ConstructXml(isinElement: ElementIsinWrongSchemeTag);
        var result = _parser.ParseXmlString(xml);
        result.Should().BeNull();
        _mockLogger.Verify(logger => logger.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
    
    [Fact]
    public void ParseXmlString_OnlyEventTypePresentAllOthersMissing_ShouldReturnNullAndLogWarning()
    {
        var xml = ConstructXml(eventTypeElement: ElementEventTypeTag, 
                               productNameFullElement: null, 
                               ibtTypeCodeElement: null, 
                               isinElement: null, 
                               includeInstrumentIdsNode: false);
        var result = _parser.ParseXmlString(xml);
        result.Should().BeNull();
        _mockLogger.Verify(logger => logger.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void ParseXmlString_AllRequiredFieldsMissingOrEmptyTags_ShouldReturnNullAndLogWarning()
    {
        var xml = ConstructXml(eventTypeElement: "<EventType></EventType>", 
                               productNameFullElement: "<ProductNameFull></ProductNameFull>", 
                               ibtTypeCodeElement: "<IBTTypeCode></IBTTypeCode>", 
                               isinElement: "<InstrumentId><IdSchemeCode>I-</IdSchemeCode><IdValue></IdValue></InstrumentId>");
        var result = _parser.ParseXmlString(xml);
        result.Should().BeNull();
        _mockLogger.Verify(logger => logger.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}