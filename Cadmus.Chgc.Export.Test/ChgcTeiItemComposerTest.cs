using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Cadmus.Chgc.Export.Test;

public sealed class ChgcTeiItemComposerTest
{
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
