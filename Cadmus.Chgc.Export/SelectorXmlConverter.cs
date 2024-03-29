﻿using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Cadmus.Chgc.Export;

public static class SelectorXmlConverter
{
    private static (double x1, double y1, double x2, double y2)
        GetPolygonBoundingBox(string points)
    {
        // get min and max for x and from points where each point is a pair
        // of coords separated by comma, and each pair is separated by space
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;
        foreach (string pair in points.Split(' '))
        {
            string[] coords = pair.Split(',');
            double x = double.Parse(coords[0], CultureInfo.InvariantCulture);
            double y = double.Parse(coords[1], CultureInfo.InvariantCulture);
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }
        return (minX, minY, maxX, maxY);
    }

    private static (double x1, double y1, double x2, double y2)
        GetEllipseBoundingBox(double cx, double cy, double rx, double ry)
    {
        // get bounding box from center (cx,cy) and axes (rx, ry)
        return (cx - rx, cy - ry, cx + rx, cy + ry);
    }

    private static void AddBoundingBoxAttrs(double x1, double y1,
        double x2, double y2, XElement target)
    {
        target.SetAttributeValue("ulx", x1);
        target.SetAttributeValue("uly", y1);
        target.SetAttributeValue("lrx", x2);
        target.SetAttributeValue("lry", y2);
    }

    public static XElement RectangleToSVG(double x, double y,
        double width, double height) =>
        new(ChgcTeiItemComposer.SVG_NS + "svg",
            new XElement(ChgcTeiItemComposer.SVG_NS + "rect",
                new XAttribute("x", x),
                new XAttribute("y", y),
                new XAttribute("width", width),
                new XAttribute("height", height)));

    public static XElement PolygonToSVG(string points) =>
        new(ChgcTeiItemComposer.SVG_NS + "svg",
            new XElement(ChgcTeiItemComposer.SVG_NS + "polygon",
                new XAttribute("points", points)));

    /// <summary>
    /// Converts the specified selector into a set of attributes on the target
    /// element.
    /// </summary>
    /// <param name="selector">The selector.</param>
    /// <param name="target">The target element.</param>
    /// <exception cref="ArgumentNullException">selector or target</exception>
    /// <exception cref="ArgumentException">Invalid selector</exception>
    public static void Convert(string selector, XElement target)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(target);

        // rectangle
        if (selector.StartsWith("xywh=pixel:", StringComparison.Ordinal))
        {
            string csv = selector[11..];
            string[] values = csv.Split(',');
            if (values.Length != 4)
                throw new ArgumentException("Invalid selector: " + selector);
            double x = double.Parse(values[0], CultureInfo.InvariantCulture);
            double y = double.Parse(values[1], CultureInfo.InvariantCulture);
            double w = double.Parse(values[2], CultureInfo.InvariantCulture);
            double h = double.Parse(values[3], CultureInfo.InvariantCulture);

            target.SetAttributeValue("ulx", x);
            target.SetAttributeValue("uly", y);
            target.SetAttributeValue("lrx", x + w);
            target.SetAttributeValue("lry", y + h);

            // replace SVG (this does not come from Annotorious)
            target.Element(ChgcTeiItemComposer.SVG_NS + "svg")?.Remove();
            target.Add(RectangleToSVG(x, y, w, h));

            return;
        }

        // else parse svg element
        if (!selector.StartsWith("<svg>", StringComparison.Ordinal))
            throw new ArgumentException("Invalid selector: " + selector);

        XElement svg = XElement.Parse(selector);
        XElement? shape = svg.Elements().FirstOrDefault();
        switch (shape?.Name?.LocalName)
        {
            // polygon: <svg><polygon points="..."/> e.g.:
            // <svg><polygon points="269,389 246,467 368,529 439,413 372,379">
            // </polygon></svg>
            case "polygon":
                target.SetAttributeValue("points",
                    shape.Attribute("points")!.Value);
                // replace SVG (this does not come from Annotorious)
                target.Element(ChgcTeiItemComposer.SVG_NS + "svg")?.Remove();
                target.Add(PolygonToSVG(shape.Attribute("points")!.Value));
                return;

            // circle: e.g.
            // <svg><circle cx=\"364.5\" cy=\"461\" r=\"141.2276530995258\">
            // </circle></svg>
            case "circle":
                double r = double.Parse(shape.Attribute("r")!.Value!,
                    CultureInfo.InvariantCulture);
                (double x1, double y1, double x2, double y2) =
                    GetEllipseBoundingBox(
                        double.Parse(shape.Attribute("cx")!.Value!,
                            CultureInfo.InvariantCulture),
                        double.Parse(shape.Attribute("cy")!.Value!,
                            CultureInfo.InvariantCulture),
                        r, r);
                AddBoundingBoxAttrs(x1, y1, x2, y2, target);

                // replace svg
                target.Element(ChgcTeiItemComposer.SVG_NS + "svg")?.Remove();
                target.Add(
                    new XElement(ChgcTeiItemComposer.SVG_NS + "svg",
                        new XElement(ChgcTeiItemComposer.SVG_NS + "circle",
                            new XAttribute("cx", shape.Attribute("cx")!.Value),
                            new XAttribute("cy", shape.Attribute("cy")!.Value),
                            new XAttribute("r", shape.Attribute("r")!.Value))));
                return;

            // ellipse: e.g.
            // <svg><ellipse cx=\"115.5\" cy=\"506\" rx=\"37.5\" ry=\"72\">
            // </ellipse></svg>
            case "ellipse":
                (x1, y1, x2, y2) = GetEllipseBoundingBox(
                    double.Parse(shape.Attribute("cx")!.Value!,
                        CultureInfo.InvariantCulture),
                    double.Parse(shape.Attribute("cy")!.Value!,
                        CultureInfo.InvariantCulture),
                    double.Parse(shape.Attribute("rx")!.Value!,
                        CultureInfo.InvariantCulture),
                    double.Parse(shape.Attribute("ry")!.Value!,
                        CultureInfo.InvariantCulture));
                AddBoundingBoxAttrs(x1, y1, x2, y2, target);

                // replace svg
                target.Element(ChgcTeiItemComposer.SVG_NS + "svg")?.Remove();
                target.Add(
                    new XElement(ChgcTeiItemComposer.SVG_NS + "svg",
                        new XElement(ChgcTeiItemComposer.SVG_NS + "ellipse",
                            new XAttribute("cx", shape.Attribute("cx")!.Value),
                            new XAttribute("cy", shape.Attribute("cy")!.Value),
                            new XAttribute("rx", shape.Attribute("rx")!.Value),
                            new XAttribute("ry", shape.Attribute("ry")!.Value))));
                break;

            // freehand: e.g.
            // <svg><path d=\"M381 44 L381 44 L381 45 L382 46 L382 47 L384 49...">
            // </path></svg>
            case "path":
                // replace svg
                target.Element(ChgcTeiItemComposer.SVG_NS + "svg")?.Remove();
                target.Add(
                    new XElement(ChgcTeiItemComposer.SVG_NS + "svg",
                        new XElement(ChgcTeiItemComposer.SVG_NS + "path",
                            new XAttribute("d", selector))));
                break;
        }
    }
}
