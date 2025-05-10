using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using InternetBasedTermsService.Domain;

namespace InternetBasedTermsService.Application.Parsing;

public class XmlParser(ILogger<XmlParser> logger)
{
    private static readonly XNamespace Ns = "http://schemas.vontobel.com/dataservice/v1.0";

    
    public IbtData? ParseFromFile(string xmlFilePath)
    {
        if (string.IsNullOrEmpty(xmlFilePath))
        {
            logger.LogError("XML file path is null or empty.");
            return null;
        }

        if (!File.Exists(xmlFilePath))
        {
            logger.LogError("XML file not found at path: {FilePath}", xmlFilePath);
            return null;
        }

        try
        {
            // Read the content of the file
            var xmlContent = File.ReadAllText(xmlFilePath);
            logger.LogDebug("Successfully read XML content from file: {FilePath}", xmlFilePath);

            // Delegate the actual parsing to ParseXmlString
            return ParseXmlString(xmlContent);
        }
        catch (IOException ioEx)
        {
            logger.LogError(ioEx, "IO error reading XML file {FilePath}.", xmlFilePath);
            return null;
        }
        catch (UnauthorizedAccessException uaEx)
        {
            logger.LogError(uaEx, "Access denied while reading XML file {FilePath}.", xmlFilePath);
            return null;
        }
        catch (Exception ex) // Catch other potential exceptions from File.ReadAllText
        {
            logger.LogError(ex, "An unexpected error occurred while reading file {FilePath}.", xmlFilePath);
            return null;
        }
    }
    
    
    public IbtData? ParseXmlString(string xmlContent)
    {
          if (string.IsNullOrEmpty(xmlContent))
          {
              logger.LogError("XML content is null or empty.");
              return null;
          }

          try
          {
              var doc = XDocument.Parse(xmlContent); // Use Parse for string content
              if (doc.Root == null)
              {
                  logger.LogError("XML document is empty or has no root element from the provided string content.");
                  return null;
              }

              var namespaceManager = new XmlNamespaceManager(new NameTable());
              namespaceManager.AddNamespace("vt", Ns.NamespaceName);

              var eventType = GetEventType(doc.Root, namespaceManager);
              var productNameFull = GetProductNameFull(doc.Root, namespaceManager);
              var ibtTypeCode = GetIbtTypeCode(doc.Root, namespaceManager);
              var isin = GetIsin(doc.Root, namespaceManager);

              if (string.IsNullOrEmpty(eventType) ||
                  string.IsNullOrEmpty(productNameFull) ||
                  string.IsNullOrEmpty(ibtTypeCode) ||
                  string.IsNullOrEmpty(isin))
              {
                  logger.LogWarning("One or more required fields could not be extracted from the XML content. " +
                                    "EventType: '{EventTypeFound}', ProductNameFull: '{ProductNameFullFound}', " +
                                    "IbtTypeCode: '{IbtTypeCodeFound}', Isin: '{IsinFound}'",
                      eventType ?? "MISSING", productNameFull ?? "MISSING",
                      ibtTypeCode ?? "MISSING", isin ?? "MISSING");
                  return null;
              }

              return new IbtData
              {
                  EventType = eventType,
                  ProductNameFull = productNameFull,
                  IbtTypeCode = ibtTypeCode,
                  Isin = isin
              };
          }
          catch (XmlException xmlEx)
          {
              logger.LogError(xmlEx, "XML parsing error from string content. The content might be malformed.");
              return null;
          }
          catch (Exception ex)
          {
              logger.LogError(ex, "An unexpected error occurred while parsing XML string content.");
              return null;
          }
    }
    
    private string? GetEventType(XElement rootElement, IXmlNamespaceResolver namespaceManager)
    {
        try
        {
            return rootElement.XPathSelectElement("./vt:Events/vt:Event/vt:EventType", namespaceManager)?.Value;
        }
        catch (XPathException ex)
        {
            logger.LogError(ex, "XPathException while trying to extract EventType.");
            return null;
        }
    }

    private string? GetProductNameFull(XElement rootElement, IXmlNamespaceResolver namespaceManager)
    {
        try
        {
            return rootElement.XPathSelectElement("./vt:Instrument/vt:ProductNameFull", namespaceManager)?.Value;
        }
        catch (XPathException ex)
        {
            logger.LogError(ex, "XPathException while trying to extract ProductNameFull.");
            return null;
        }
    }
    
    private string? GetIbtTypeCode(XElement rootElement, IXmlNamespaceResolver namespaceManager)
    {
        try
        {
            return rootElement.XPathSelectElement("./vt:Instrument/vt:IBTTypeCode", namespaceManager)?.Value;
        }
        catch (XPathException ex)
        {
            logger.LogError(ex, "XPathException while trying to extract IBTTypeCode.");
            return null;
        }
    }

    /// <summary>
    /// Extracts the ISIN from the XML.
    /// This is the IdValue where IdSchemeCode is 'I-'.
    /// Path: /IBTTermSheet/Instrument/InstrumentIds/InstrumentId[IdSchemeCode='I-']/IdValue
    /// </summary>
    private string? GetIsin(XElement rootElement, IXmlNamespaceResolver namespaceManager)
    {
        try
        {
            // Using XPath to find the InstrumentId with specific IdSchemeCode
            // The .// ensures it searches descendants if InstrumentIds is not a direct child of Instrument (though it is in the sample)
            return rootElement.XPathSelectElement(".//vt:InstrumentId[vt:IdSchemeCode='I-']/vt:IdValue", namespaceManager)?.Value;
        }
        catch (XPathException ex)
        {
            logger.LogError(ex, "XPathException while trying to extract ISIN.");
            return null;
        }
    }
}