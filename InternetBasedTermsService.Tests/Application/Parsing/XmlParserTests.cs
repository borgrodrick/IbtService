using InternetBasedTermsService.Application.Parsing;

namespace InternetBasedTermsService.Tests.Infrastructure;

using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions; // For NullLogger
using System.IO;
using Microsoft.Extensions.Logging;


public class XmlParserTests
{
    private readonly XmlParser _parser;
    private readonly ILogger<XmlParser> _logger;

    public XmlParserTests()
    {
        // Using NullLogger to avoid actual logging output during tests,
        // unless you specifically want to test logging behavior.
        _logger = NullLogger<XmlParser>.Instance;
        _parser = new XmlParser(_logger);
    }

    private string CreateTempXmlFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content);
        return tempFile;
    }

    [Fact]
    public void Parse_WithValidXmlFile_ShouldReturnParsedData()
    {
        // Arrange
        // Assuming IBT.xml is in TestData and copied to output
        string validXmlPath = Path.Combine("TestData", "IBT.xml");
        File.Exists(validXmlPath).Should().BeTrue("Test precondition: Valid XML file must exist.");

        // Act
        var result = _parser.Parse(validXmlPath);

        // Assert
        result.Should().NotBeNull();
        result.EventType.Should().Be("9097");
        result.ProductNameFull.Should().Be("24.55% p.a. Barrier Reverse Convertible on Bloom Energy Corp, Walt Disney");
        result.IbtTypeCode.Should().Be("100060");
        result.Isin.Should().Be("CH1437701258");
    }

    [Fact]
    public void Parse_WithMissingEventType_ShouldReturnNull()
    {
        // Arrange
        var xmlContent = @"
            <IBTTermSheet xmlns='http://schemas.vontobel.com/dataservice/v1.0'>
                <Instrument>
                    <ProductNameFull>Test Product</ProductNameFull>
                    <IBTTypeCode>123</IBTTypeCode>
                    <InstrumentIds><InstrumentId><IdSchemeCode>I-</IdSchemeCode><IdValue>ISIN123</IdValue></InstrumentId></InstrumentIds>
                </Instrument>
                <Events><Event></Event></Events> {/* Missing EventType */}
            </IBTTermSheet>";
        var tempFile = CreateTempXmlFile(xmlContent);

        // Act
        var result = _parser.Parse(tempFile);

        // Assert
        result.Should().BeNull();
        File.Delete(tempFile); // Cleanup
    }

    [Fact]
    public void Parse_WithMissingIsinWithCorrectScheme_ShouldReturnNull()
    {
        // Arrange
        var xmlContent = @"
            <IBTTermSheet xmlns='http://schemas.vontobel.com/dataservice/v1.0'>
                <Events><Event><EventType>9097</EventType></Event></Events>
                <Instrument>
                    <ProductNameFull>Test Product</ProductNameFull>
                    <IBTTypeCode>123</IBTTypeCode>
                    <InstrumentIds>
                         <InstrumentId><IdSchemeCode>CH</IdSchemeCode><IdValue>SOMEOTHERID</IdValue></InstrumentId>
                    </InstrumentIds> {/* No InstrumentId with IdSchemeCode 'I-' */}
                </Instrument>
            </IBTTermSheet>";
        var tempFile = CreateTempXmlFile(xmlContent);

        // Act
        var result = _parser.Parse(tempFile);

        // Assert
        result.Should().BeNull();
        File.Delete(tempFile); // Cleanup
    }


    [Fact]
    public void Parse_WithMalformedXml_ShouldReturnNull()
    {
        // Arrange
        var xmlContent = "<IBTTermSheet><UnclosedTag</IBTTermSheet>";
        var tempFile = CreateTempXmlFile(xmlContent);

        // Act
        var result = _parser.Parse(tempFile);

        // Assert
        result.Should().BeNull();
        File.Delete(tempFile); // Cleanup
    }

    [Fact]
    public void Parse_WithNonExistentFile_ShouldReturnNull()
    {
        // Arrange
        var nonExistentFilePath = "non_existent_file.xml";

        // Act
        var result = _parser.Parse(nonExistentFilePath);

        // Assert
        result.Should().BeNull();
    }
}