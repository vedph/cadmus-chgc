using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Cadmus.Chgc.Export.Test;

public class SelectorXmlConverterTest
{
    private static XElement GetZone() =>
        new(ChgcTeiItemComposer.TEI_NS + "zone", new XAttribute("id", "test"));

    private static double GetDouble(XElement zone, string name) =>
        double.Parse(zone.Attribute(name)!.Value, CultureInfo.InvariantCulture);

    private static XElement GetSvgShape(XElement zone) =>
        zone.Element(ChgcTeiItemComposer.SVG_NS + "svg")!.Elements().Single();

    [Fact]
    public void Convert_JsonRectangle_Ok()
    {
        XElement zone = GetZone();

        // the selector of the reported export error
        SelectorXmlConverter.Convert("{\"type\":\"RECTANGLE\",\"geometry\":" +
            "{\"bounds\":{\"minX\":173,\"minY\":723,\"maxX\":507,\"maxY\":1379}," +
            "\"rot\":0,\"x\":173,\"y\":723,\"w\":334,\"h\":656}}", zone);

        Assert.Equal("173", zone.Attribute("ulx")?.Value);
        Assert.Equal("723", zone.Attribute("uly")?.Value);
        Assert.Equal("507", zone.Attribute("lrx")?.Value);
        Assert.Equal("1379", zone.Attribute("lry")?.Value);
        XElement rect = GetSvgShape(zone);
        Assert.Equal("rect", rect.Name.LocalName);
        Assert.Equal("334", rect.Attribute("width")?.Value);
        Assert.Equal("656", rect.Attribute("height")?.Value);
    }

    [Fact]
    public void Convert_JsonRotatedRectangle_Ok()
    {
        XElement zone = GetZone();

        // 20x10 rectangle centered at 20,10, rotated by 90 degrees
        SelectorXmlConverter.Convert("{\"type\":\"RECTANGLE\",\"geometry\":" +
            "{\"x\":10,\"y\":5,\"w\":20,\"h\":10,\"rot\":" +
            (Math.PI / 2).ToString("R", CultureInfo.InvariantCulture) + "}}", zone);

        Assert.Equal(15, GetDouble(zone, "ulx"), 6);
        Assert.Equal(0, GetDouble(zone, "uly"), 6);
        Assert.Equal(25, GetDouble(zone, "lrx"), 6);
        Assert.Equal(20, GetDouble(zone, "lry"), 6);
        Assert.Equal("rotate(90 20 10)",
            GetSvgShape(zone).Attribute("transform")?.Value);
    }

    [Fact]
    public void Convert_JsonPolygon_Ok()
    {
        XElement zone = GetZone();

        SelectorXmlConverter.Convert("{\"type\":\"POLYGON\",\"geometry\":" +
            "{\"points\":[[10.5,20],[30,40],[50,5]],\"bounds\":{}}}", zone);

        Assert.Equal("10.5,20 30,40 50,5", zone.Attribute("points")?.Value);
        Assert.Equal("polygon", GetSvgShape(zone).Name.LocalName);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{}")]
    [InlineData("{\"type\":\"RECTANGLE\",\"geometry\":{\"x\":1}}")]
    [InlineData("{\"type\":\"POLYGON\",\"geometry\":{\"points\":[[1]]}}")]
    [InlineData("{\"type\":\"UNKNOWN\",\"geometry\":{}}")]
    [InlineData("nonsense")]
    public void Convert_Invalid_Throws(string selector)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => SelectorXmlConverter.Convert(selector, GetZone()));
        Assert.Contains("Invalid selector", ex.Message);
    }

    [Fact]
    public void Convert_SvgRectangle_Ok()
    {
        XElement zone = GetZone();

        SelectorXmlConverter.Convert(
            "<svg><rect x=\"10\" y=\"20\" width=\"30\" height=\"40\" /></svg>",
            zone);

        Assert.Equal("10", zone.Attribute("ulx")?.Value);
        Assert.Equal("20", zone.Attribute("uly")?.Value);
        Assert.Equal("40", zone.Attribute("lrx")?.Value);
        Assert.Equal("60", zone.Attribute("lry")?.Value);
        Assert.Null(GetSvgShape(zone).Attribute("transform"));
    }

    [Fact]
    public void Convert_SvgRotatedRectangle_Ok()
    {
        XElement zone = GetZone();

        SelectorXmlConverter.Convert("<svg><rect x=\"10\" y=\"5\" width=\"20\" " +
            "height=\"10\" transform=\"rotate(90 20 10)\" /></svg>", zone);

        Assert.Equal(15, GetDouble(zone, "ulx"), 6);
        Assert.Equal(0, GetDouble(zone, "uly"), 6);
        Assert.Equal(25, GetDouble(zone, "lrx"), 6);
        Assert.Equal(20, GetDouble(zone, "lry"), 6);
        Assert.Equal("rotate(90 20 10)",
            GetSvgShape(zone).Attribute("transform")?.Value);
    }

    [Fact]
    public void Convert_Path_Ok()
    {
        XElement zone = GetZone();

        SelectorXmlConverter.Convert(
            "<svg><path d=\"M381 44 L382 46\"></path></svg>", zone);

        Assert.Equal("M381 44 L382 46", GetSvgShape(zone).Attribute("d")?.Value);
    }

    [Fact]
    public void Convert_SelfClosedEllipse_Ok()
    {
        XElement zone = GetZone();

        // as serialized by Annotorious V3
        SelectorXmlConverter.Convert(
            "<svg><ellipse cx=\"100.5\" cy=\"50\" rx=\"10\" ry=\"5\" /></svg>", zone);

        Assert.Equal("90.5", zone.Attribute("ulx")?.Value);
        Assert.Equal("45", zone.Attribute("uly")?.Value);
        Assert.Equal("110.5", zone.Attribute("lrx")?.Value);
        Assert.Equal("55", zone.Attribute("lry")?.Value);
        Assert.Equal("ellipse", GetSvgShape(zone).Name.LocalName);
    }

    [Fact]
    public void Convert_Rectangle_Ok()
    {
        XElement zone = new(ChgcTeiItemComposer.TEI_NS + "zone",
            new XAttribute("id", "test"));

        SelectorXmlConverter.Convert("xywh=pixel:10,20,300,400", zone);

        Assert.Equal(5, zone.Attributes().Count());

        Assert.Equal("10", zone.Attribute("ulx")?.Value);
        Assert.Equal("20", zone.Attribute("uly")?.Value);
        Assert.Equal("310", zone.Attribute("lrx")?.Value);
        Assert.Equal("420", zone.Attribute("lry")?.Value);
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

        // svg
        Assert.NotNull(zone.Element(ChgcTeiItemComposer.SVG_NS + "svg"));
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

        // svg
        Assert.NotNull(zone.Element(ChgcTeiItemComposer.SVG_NS + "svg"));
    }
}
