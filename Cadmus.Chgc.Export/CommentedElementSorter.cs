using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Cadmus.Chgc.Export;

/// <summary>
/// Utility class used to sort a set of child elements by some criterion,
/// while preserving the comments preceding them if any.
/// </summary>
public static class CommentedElementSorter
{
    private static XComment? GetComment(XElement target)
    {
        // get the leftmost comment for target skipping non significant whitespaces
        XNode? node = target.PreviousNode;
        while (node != null)
        {
            if (node.NodeType == XmlNodeType.Comment) return (XComment)node;
            if (node.NodeType == XmlNodeType.Text)
            {
                string text = ((XText)node).Value;
                if (!string.IsNullOrWhiteSpace(text)) break;
                node = node.PreviousNode;
            }
        }
        return null;
    }

    public static XElement SortChildElements(XElement parent,
        Func<XElement,string> GetSortKey)
    {
        var sorted = parent.Elements()
            .Select(e => new CommentedElement(GetSortKey(e), e, GetComment(e)))
            .OrderBy(e => e.SortKey)
            .ToList();

        var result = new XElement(parent.Name);
        foreach (CommentedElement ce in sorted)
        {
            if (ce.Comment != null) result.Add(ce.Comment);
            result.Add(ce.Element);
        }
        return result;
    }
}

internal class CommentedElement
{
    public string SortKey { get; }
    public XElement Element { get; }
    public XComment? Comment { get; }

    public CommentedElement(string sortKey, XElement element, XComment? comment)
    {
        SortKey = sortKey;
        Element = element;
        Comment = comment;
    }

    public override string ToString()
    {
        return SortKey + ": " + Element.ToString(SaveOptions.DisableFormatting);
    }
}
