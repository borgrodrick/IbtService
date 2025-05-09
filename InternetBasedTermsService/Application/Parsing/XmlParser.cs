using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using InternetBasedTermsService.Domain;

namespace InternetBasedTermsService.Application.Parsing;

public class XmlParser(ILogger<XmlParser> logger)
{
    private static readonly XNamespace Ns = "http://schemas.vontobel.com/dataservice/v1.0";

    public IbtData? Parse(string xmlFilePath)
    {
        IbtData? parsedData = null;

        try
        {
            var doc = XDocument.Load(xmlFilePath);

            var namespaceManager = new XmlNamespaceManager(new NameTable());
            namespaceManager.AddNamespace("vt", Ns.NamespaceName);

            var eventType = doc.Root?.XPathSelectElement("./vt:Events/vt:Event/vt:EventType", namespaceManager)?.Value;
            var productNameFull = doc.Root?.XPathSelectElement("./vt:Instrument/vt:ProductNameFull", namespaceManager)?.Value;
            var ibtTypeCode = doc.Root?.XPathSelectElement("./vt:Instrument/vt:IBTTypeCode", namespaceManager)?.Value;
            var isin = doc.Root?.XPathSelectElement(".//vt:InstrumentId[vt:IdSchemeCode='I-']/vt:IdValue", namespaceManager)?.Value;

            if (!string.IsNullOrEmpty(eventType) && !string.IsNullOrEmpty(productNameFull) &&
                !string.IsNullOrEmpty(ibtTypeCode) && !string.IsNullOrEmpty(isin))
            {
                parsedData = new IbtData
                {
                    EventType = eventType,
                    ProductNameFull = productNameFull,
                    IbtTypeCode = ibtTypeCode,
                    Isin = isin
                };
            }
            else
            {
                logger.LogError("XML Parsing Error: One or more required fields missing in file {FilePath}.", xmlFilePath);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception during XML parsing of file {FilePath}.", xmlFilePath);
        }

        return parsedData;
    }
}