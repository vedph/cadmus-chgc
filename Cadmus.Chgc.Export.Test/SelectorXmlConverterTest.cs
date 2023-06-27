using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Cadmus.Chgc.Export.Test;

public class SelectorXmlConverterTest
{
    [Fact]
    public void Convert_Rectangle_Ok()
    {
        XElement zone = new(ChgcTeiItemComposer.TEI_NS + "zone",
            new XAttribute("id", "test"));

        SelectorXmlConverter.Convert("xywh=pixel:10,20,300,400", zone);

        Assert.Equal(5, zone.Attributes().Count());

        Assert.Equal("10", zone.Attribute("ulx")?.Value);
        Assert.Equal("20", zone.Attribute("uly")?.Value);
        Assert.Equal("300", zone.Attribute("lrx")?.Value);
        Assert.Equal("400", zone.Attribute("lry")?.Value);
    }

    [Fact]
    public void Convert_Polygon_Ok()
    {
        XElement zone = new(ChgcTeiItemComposer.TEI_NS + "zone",
            new XAttribute("id", "test"));

        const string points = "269,389 246,467 368,529 439,413 372,379";
        SelectorXmlConverter.Convert("<svg><polygon " +
            $"points=\"{points}\"></polygon></svg>",
            zone);

        Assert.Equal(2, zone.Attributes().Count());

        Assert.Equal(points, zone.Attribute("points")?.Value);
    }

    [Fact]
    public void Convert_Circle_Ok()
    {
        XElement zone = new(ChgcTeiItemComposer.TEI_NS + "zone",
            new XAttribute("id", "test"));

        const string selector = "<svg><circle cx=\"100\" cy=\"50\" r=\"10\">" +
            "</circle></svg>";
        SelectorXmlConverter.Convert(selector, zone);

        // bbox
        Assert.Equal(5, zone.Attributes().Count());
        Assert.Equal("90", zone.Attribute("ulx")?.Value);
        Assert.Equal("40", zone.Attribute("uly")?.Value);
        Assert.Equal("110", zone.Attribute("lrx")?.Value);
        Assert.Equal("60", zone.Attribute("lry")?.Value);

        // comment
        Assert.Single(zone.Nodes());
        Assert.Equal($"<!--{selector}-->", zone.Nodes().First().ToString());
    }

    [Fact]
    public void Convert_Ellipse_Ok()
    {
        XElement zone = new(ChgcTeiItemComposer.TEI_NS + "zone",
            new XAttribute("id", "test"));

        const string selector = "<svg><ellipse cx=\"100\" cy=\"50\" " +
            "rx=\"10\" ry=\"5\"></ellipse></svg>";
        SelectorXmlConverter.Convert(selector, zone);

        // bbox
        Assert.Equal(5, zone.Attributes().Count());
        Assert.Equal("90", zone.Attribute("ulx")?.Value);
        Assert.Equal("45", zone.Attribute("uly")?.Value);
        Assert.Equal("110", zone.Attribute("lrx")?.Value);
        Assert.Equal("55", zone.Attribute("lry")?.Value);

        // comment
        Assert.Single(zone.Nodes());
        Assert.Equal($"<!--{selector}-->", zone.Nodes().First().ToString());
    }
}
