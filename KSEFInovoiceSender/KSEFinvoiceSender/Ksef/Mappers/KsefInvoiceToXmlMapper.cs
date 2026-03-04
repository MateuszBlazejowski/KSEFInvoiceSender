using KSEFinvoiceSender.Domain.Entities;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace KSEFinvoiceSender.Ksef.Mappers;

public static class KsefInvoiceToXmlMapper
{
    public static byte[] SerializeInvoiceToXml(Faktura invoice)
    {
        var serializer = new XmlSerializer(typeof(Faktura));

        var namespaces = new XmlSerializerNamespaces();
        namespaces.Add("", "http://crd.gov.pl/wzor/2025/06/25/13775/");
        namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
        namespaces.Add("xsd", "http://www.w3.org/2001/XMLSchema");
       
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false), // UTF-8 without BOM (perfect for KSeF)
            Indent = false,                     // Minified XML for production
            OmitXmlDeclaration = false          // Keeps <?xml version="1.0" encoding="utf-8"?>
        };

        using var ms = new MemoryStream();

  
        using (var writer = XmlWriter.Create(ms, settings))
        {
            serializer.Serialize(writer, invoice, namespaces);
        }

        return ms.ToArray();
    }
}