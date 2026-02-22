using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Compze.Build.FlexRef.SystemCE;

static class XDocumentCE
{
    public static void SaveWithoutDeclaration(this XDocument @this, string filePath)
    {
        var writerSettings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = true,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };

        using var stream = File.Create(filePath);
        using var writer = XmlWriter.Create(stream, writerSettings);
        @this.Save(writer);
    }
}
