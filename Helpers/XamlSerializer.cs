using System.IO;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml;

namespace FlashMemo.Helpers;

public static class XamlSerializer
{
    public static string ToXaml(FlowDocument doc)
    {
        return XamlWriter.Save(doc);
    }

    public static FlowDocument FromXaml(string xaml)
    {
        if (string.IsNullOrWhiteSpace(xaml))
            return new FlowDocument();

        using var stringReader = new StringReader(xaml);
        using var xmlReader = XmlReader.Create(stringReader);

        return (FlowDocument)XamlReader.Load(xmlReader);
    }

    public static string GetPlainText(FlowDocument doc)
    {
        return new TextRange(
            doc.ContentStart,
            doc.ContentEnd).Text.Trim();
    }

}