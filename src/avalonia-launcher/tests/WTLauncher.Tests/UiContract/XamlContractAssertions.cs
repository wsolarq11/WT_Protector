using System.Xml.Linq;

namespace WTLauncher.Tests.UiContract;

internal static class XamlContractAssertions
{
    public static XDocument Load(string path) => XDocument.Load(path, LoadOptions.None);

    public static void AssertAttribute(XDocument doc, string attributeName, string attributeValue)
    {
        var found = Elements(doc)
            .Attributes()
            .Any(a => a.Name.LocalName == attributeName
                && string.Equals(a.Value, attributeValue, StringComparison.Ordinal));
        Assert.True(found, $"Expected XAML attribute {attributeName}=\"{attributeValue}\".");
    }

    public static void AssertNoAttribute(XDocument doc, string attributeName, string attributeValue)
    {
        var found = Elements(doc)
            .Attributes()
            .Any(a => a.Name.LocalName == attributeName
                && string.Equals(a.Value, attributeValue, StringComparison.Ordinal));
        Assert.False(found, $"Unexpected XAML attribute {attributeName}=\"{attributeValue}\".");
    }

    public static void AssertElement(XDocument doc, string elementName, params (string Attribute, string Value)[] attributes)
    {
        Assert.True(HasElement(doc, elementName, attributes),
            $"Expected XAML element {elementName} with the specified attributes.");
    }

    public static void AssertNoElement(XDocument doc, string elementName, params (string Attribute, string Value)[] attributes)
    {
        Assert.False(HasElement(doc, elementName, attributes),
            $"Unexpected XAML element {elementName} with the specified attributes.");
    }

    private static IEnumerable<XElement> Elements(XDocument doc) =>
        doc.Root?.DescendantsAndSelf() ?? Enumerable.Empty<XElement>();

    private static bool HasElement(XDocument doc, string elementName, params (string Attribute, string Value)[] attributes)
    {
        return doc.Descendants().Any(element =>
            element.Name.LocalName == elementName
            && attributes.All(pair => element.Attributes().Any(attr =>
                attr.Name.LocalName == pair.Attribute
                && string.Equals(attr.Value, pair.Value, StringComparison.Ordinal))));
    }
}
