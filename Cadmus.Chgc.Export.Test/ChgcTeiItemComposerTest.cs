using Cadmus.Chgc.Parts;
using Cadmus.Core;
using Cadmus.Img.Parts;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Cadmus.Chgc.Export.Test;

public sealed class ChgcTeiItemComposerTest
{
    private static IItem GetMockItem()
    {
        IItem item = new Item
        {
            Title = "Sample",
            Description = "Sample item",
            FacetId = "default",
            CreatorId = "zeus",
            UserId = "zeus",
            GroupId = "ccc-ms029",
            SortKey = "sample"
        };
        ChgcImageAnnotationsPart part = new()
        {
            ItemId = item.Id,
            UserId = "zeus",
            CreatorId = "zeus",
            Image = new GalleryImage
            {
                Id = "1",
                Title = "1r",
                Description = "Page 1 recto",
                Uri = "http://img.org/1r.jpg"
            }
        };
        item.Parts.Add(part);
        return item;
    }

    private static void AssertTeiSkeleton(XDocument doc)
    {
        // <TEI>
        Assert.NotNull(doc.Root);
        Assert.Equal("TEI", doc.Root.Name.LocalName);

        // <teiHeader>
        Assert.NotNull(doc.Root.Element(ChgcTeiItemComposer.TEI_NS + "teiHeader"));

        // <text>
        Assert.NotNull(doc.Root.Element(ChgcTeiItemComposer.TEI_NS + "text"));
    }

    [Fact]
    public void Compose_Create_SinglePage()
    {
        IItem item = GetMockItem();
        ChgcImageAnnotationsPart part = (ChgcImageAnnotationsPart)item.Parts[0];

        // rect with n-aaron
        part.Annotations.Add(new ChgcImageAnnotation
        {
            Id = "#36c9730c-a7c9-4a28-8889-8d6015ee14fe",
            Eid = "n-aaron",
            Label = "Aaron",
            Note = "A note about Aaron",
            Target = part.Image,
            Selector = "xywh=pixel:100,50,130,70"
        });

        // circle with n-abacuc
        part.Annotations.Add(new ChgcImageAnnotation
        {
            Id = "#af35f367-a921-4af3-bb84-4911cbc82a53",
            Eid = "n-abacuc",
            Label = "Abacuc",
            Target = part.Image,
            Selector = "<svg><circle cx=\"400\" cy=\"200\" r=\"50\"></circle></svg>"
        });

        RamChgcTeiItemComposer composer = new();
        DocItemComposition output = new();
        composer.Open(output);
        composer.Compose(item);
        composer.Close();

        Assert.NotNull(output.Document);
        XDocument doc = output.Document;
        AssertTeiSkeleton(doc);

        // <facsimile>
        XElement? facsimile = doc.Root!.Element(
            ChgcTeiItemComposer.TEI_NS + "facsimile");
        Assert.NotNull(facsimile);

        // <surface> in facsimile
        Assert.Single(facsimile.Elements());
        XElement? surface = facsimile!.Element(
            ChgcTeiItemComposer.TEI_NS + "surface");
        Assert.NotNull(surface);
        Assert.Equal("#" + item.Id, surface!
            .Attribute(ChgcTeiItemComposer.XML_NS + "id")?.Value);
        Assert.Equal("ccc-ms029/1", surface!.Attribute("n")?.Value);
        Assert.Equal(part.Image!.Uri, surface!.Attribute("source")?.Value);

        // surface has 2 zones
        Assert.Equal(2, surface!.Elements().Count());

        // zone 1
        XElement? zone = surface!.Elements().First();
        Assert.NotNull(zone);
        Assert.Equal("zone", zone.Name.LocalName);
        Assert.Equal(part.Annotations[0].Id,
            zone.Attribute(ChgcTeiItemComposer.XML_NS + "id")?.Value);
        Assert.Equal("ccc-ms029/1/n-aaron", zone.Attribute("n")?.Value);
        Assert.Equal("100", zone.Attribute("ulx")?.Value);
        Assert.Equal("50", zone.Attribute("uly")?.Value);
        Assert.Equal("230", zone.Attribute("lrx")?.Value);
        Assert.Equal("120", zone.Attribute("lry")?.Value);

        // zone 1 svg
        XElement? svg = zone!.Element(ChgcTeiItemComposer.SVG_NS + "svg");
        Assert.NotNull(svg);
        Assert.Equal("100", svg!.Attribute("x")?.Value);
        Assert.Equal("50", svg!.Attribute("y")?.Value);
        Assert.Equal("130", svg!.Attribute("width")?.Value);
        Assert.Equal("70", svg!.Attribute("height")?.Value);

        // zone 2
        zone = surface!.Elements().Last();
        Assert.NotNull(zone);
        Assert.Equal("zone", zone.Name.LocalName);
        Assert.Equal(part.Annotations[1].Id,
            zone.Attribute(ChgcTeiItemComposer.XML_NS + "id")?.Value);
        Assert.Equal("ccc-ms029/1/n-abacuc", zone.Attribute("n")?.Value);
        Assert.Equal("350", zone.Attribute("ulx")?.Value);
        Assert.Equal("150", zone.Attribute("uly")?.Value);
        Assert.Equal("450", zone.Attribute("lrx")?.Value);
        Assert.Equal("250", zone.Attribute("lry")?.Value);

        // zone 2 svg
        svg = zone!.Element(ChgcTeiItemComposer.SVG_NS + "svg");
        Assert.NotNull(svg);
        Assert.Single(svg!.Elements());
        XElement? circle = svg!.Element(ChgcTeiItemComposer.SVG_NS + "circle");
        Assert.NotNull(circle);
        Assert.Equal("400", circle!.Attribute("cx")?.Value);
        Assert.Equal("200", circle!.Attribute("cy")?.Value);
        Assert.Equal("50", circle!.Attribute("r")?.Value);
    }

    [Fact]
    public void InsertInOrder_NoChildren_Added()
    {
        XElement parent = new("parent");
        XElement child = new("child", new XAttribute("n", "a"));
        
        ChgcTeiItemComposer.InsertInOrder(parent, child, "n");

        Assert.Single(parent.Elements());
        Assert.Same(child, parent.Elements().First());
    }

    [Fact]
    public void InsertInOrder_Children_AddedBefore()
    {
        XElement parent = new("parent");
        XElement a = new("child", new XAttribute("n", "a"));
        XElement b = new("child", new XAttribute("n", "b"));
        parent.Add(b);

        ChgcTeiItemComposer.InsertInOrder(parent, a, "n");

        Assert.Equal(2, parent.Elements().Count());
        Assert.Same(a, parent.Elements().First());
        Assert.Same(b, parent.Elements().Last());
    }

    [Fact]
    public void InsertInOrder_Children_AddedAfter()
    {
        XElement parent = new("parent");
        XElement a = new("child", new XAttribute("n", "a"));
        XElement b = new("child", new XAttribute("n", "b"));
        parent.Add(a);

        ChgcTeiItemComposer.InsertInOrder(parent, b, "n");

        Assert.Equal(2, parent.Elements().Count());
        Assert.Same(a, parent.Elements().First());
        Assert.Same(b, parent.Elements().Last());
    }

    [Fact]
    public void InsertInOrder_NoChildrenSection1_Added()
    {
        XElement pba = new("pb", new XAttribute("n", "a"));
        XElement pbb = new("pb", new XAttribute("n", "b"));
        XElement parent = new("parent", pba, pbb);
        XElement a1 = new("child", new XAttribute("n", "a1"));

        ChgcTeiItemComposer.InsertInOrder(parent, a1, "n", pba, pbb);

        Assert.Equal(3, parent.Elements().Count());
        List<XElement> children = parent.Elements().ToList();
        Assert.Same(pba, children[0]);
        Assert.Same(a1, children[1]);
        Assert.Same(pbb, children[2]);
    }

    [Fact]
    public void InsertInOrder_NoChildrenSection2_Added()
    {
        XElement pba = new("pb", new XAttribute("n", "a"));
        XElement pbb = new("pb", new XAttribute("n", "b"));
        XElement parent = new("parent", pba, pbb);
        XElement b1 = new("child", new XAttribute("n", "b1"));

        ChgcTeiItemComposer.InsertInOrder(parent, b1, "n", pbb);

        Assert.Equal(3, parent.Elements().Count());
        List<XElement> children = parent.Elements().ToList();
        Assert.Same(pba, children[0]);
        Assert.Same(pbb, children[1]);
        Assert.Same(b1, children[2]);
    }

    [Fact]
    public void InsertInOrder_ChildrenSection1_Added()
    {
        XElement pba = new("pb", new XAttribute("n", "a"));
        XElement pbb = new("pb", new XAttribute("n", "b"));
        XElement a1 = new("child", new XAttribute("n", "a1"));
        XElement a2 = new("child", new XAttribute("n", "a2"));
        XElement parent = new("parent", pba, a1, pbb);

        ChgcTeiItemComposer.InsertInOrder(parent, a2, "n", pba, pbb);

        Assert.Equal(4, parent.Elements().Count());
        List<XElement> children = parent.Elements().ToList();
        Assert.Same(pba, children[0]);
        Assert.Same(a1, children[1]);
        Assert.Same(a2, children[2]);
        Assert.Same(pbb, children[3]);
    }

    [Fact]
    public void InsertInOrder_ChildrenSection2_Added()
    {
        XElement pba = new("pb", new XAttribute("n", "a"));
        XElement pbb = new("pb", new XAttribute("n", "b"));
        XElement b1 = new("child", new XAttribute("n", "b1"));
        XElement b2 = new("child", new XAttribute("n", "b2"));
        XElement parent = new("parent", pba, pbb, b1);

        ChgcTeiItemComposer.InsertInOrder(parent, b2, "n", pbb);

        Assert.Equal(4, parent.Elements().Count());
        List<XElement> children = parent.Elements().ToList();
        Assert.Same(pba, children[0]);
        Assert.Same(pbb, children[1]);
        Assert.Same(b1, children[2]);
        Assert.Same(b2, children[3]);
    }
}
