using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Cadmus.Chgc.Export.Test;

public sealed class CommentedElementSorterTest
{
    [Fact]
    public void SortChildElements_Ok()
    {
        XElement parent = new("parent",
            new XElement("gamma", new XAttribute("id", "g")),
            new XComment("alpha-comment"),
            new XElement("alpha", new XAttribute("id", "a")),
            new XComment("beta-comment"),
            new XElement("beta", new XAttribute("id", "b")));

        XElement result = CommentedElementSorter.SortChildElements(parent,
            e => e.Attribute("id")!.Value);

        Assert.NotNull(result);
        Assert.Equal("parent", result.Name);
        IList<XElement> children = result.Elements().ToList();
        IList<XNode> nodes = result.Nodes().ToList();
        Assert.Equal(3, children.Count);
        Assert.Equal(5, nodes.Count);

        Assert.Equal("alpha-comment", (nodes[0] as XComment)?.Value);
        Assert.Equal("alpha", children[0].Name);
        Assert.Equal("beta-comment", (nodes[2] as XComment)?.Value);
        Assert.Equal("beta", children[1].Name);
        Assert.Equal("gamma", children[2].Name);
    }
}
