using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Cadmus.Chgc.Export;

/// <summary>
/// Converter from Annotorious selectors to TEI zone attributes and SVG.
/// A selector is either a W3C media fragment for rectangles
/// (<c>xywh=pixel:x,y,w,h</c>), or an SVG fragment (<c>&lt;svg&gt;...</c>)
/// with a polygon, circle, ellipse, rectangle or path. Also, a JSON-serialized
/// Annotorious V3 rectangle or polygon is accepted, as this is how some versions
/// of the frontend stored selectors.
/// </summary>
public static partial class SelectorXmlConverter
{
    private const string NUM = @"[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?";

    [GeneratedRegex(@"^\s*rotate\(\s*(" + NUM + @")(?:[\s,]+(" + NUM +
        @")[\s,]+(" + NUM + @"))?\s*\)\s*$")]
    private static partial Regex RotateRegex();

    private static string FormatNumber(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);

    private static double ParseNumber(string value) =>
        double.Parse(value, CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts a JSON-serialized Annotorious V3 shape into the corresponding
    /// selector.
    /// </summary>
    /// <param name="json">The JSON code.</param>
    /// <returns>The selector.</returns>
    /// <exception cref="ArgumentException">Invalid selector</exception>
    private static string JsonShapeToSelector(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement geometry = doc.RootElement.GetProperty("geometry");

            switch (doc.RootElement.GetProperty("type").GetString())
            {
                case "RECTANGLE":
                    double x = geometry.GetProperty("x").GetDouble();
                    double y = geometry.GetProperty("y").GetDouble();
                    double w = geometry.GetProperty("w").GetDouble();
                    double h = geometry.GetProperty("h").GetDouble();
                    double rot = geometry.TryGetProperty("rot", out JsonElement r)
                        ? r.GetDouble() : 0;

                    if (rot == 0)
                    {
                        return $"xywh=pixel:{FormatNumber(x)},{FormatNumber(y)}," +
                            $"{FormatNumber(w)},{FormatNumber(h)}";
                    }
                    return $"<svg><rect x=\"{FormatNumber(x)}\" " +
                        $"y=\"{FormatNumber(y)}\" " +
                        $"width=\"{FormatNumber(w)}\" " +
                        $"height=\"{FormatNumber(h)}\" " +
                        $"transform=\"rotate({FormatNumber(rot * 180 / Math.PI)} " +
                        $"{FormatNumber(x + w / 2)} {FormatNumber(y + h / 2)})\"" +
                        "></rect></svg>";

                case "POLYGON":
                    string points = string.Join(" ",
                        geometry.GetProperty("points").EnumerateArray().Select(
                            p => FormatNumber(p[0].GetDouble()) + "," +
                                 FormatNumber(p[1].GetDouble())));
                    return $"<svg><polygon points=\"{points}\"></polygon></svg>";
            }
        }
        catch (Exception ex) when (ex is JsonException
            or KeyNotFoundException or InvalidOperationException
            or IndexOutOfRangeException or FormatException)
        {
            throw new ArgumentException("Invalid selector: " + json, ex);
        }
        throw new ArgumentException("Invalid selector: " + json);
    }

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

    private static (double x1, double y1, double x2, double y2)
        GetRectangleBoundingBox(double x, double y, double w, double h,
        string? transform)
    {
        Match m = RotateRegex().Match(transform ?? "");
        if (!m.Success) return (x, y, x + w, y + h);

        // SVG rotation: the center defaults to the origin
        double radians = ParseNumber(m.Groups[1].Value) * Math.PI / 180;
        double cx = m.Groups[2].Success ? ParseNumber(m.Groups[2].Value) : 0;
        double cy = m.Groups[3].Success ? ParseNumber(m.Groups[3].Value) : 0;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);

        (double X, double Y)[] corners =
        [
            (x, y), (x + w, y), (x + w, y + h), (x, y + h)
        ];
        double[] xs = [.. corners.Select(
            c => cx + (c.X - cx) * cos - (c.Y - cy) * sin)];
        double[] ys = [.. corners.Select(
            c => cy + (c.X - cx) * sin + (c.Y - cy) * cos)];
        return (xs.Min(), ys.Min(), xs.Max(), ys.Max());
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

        // JSON-serialized Annotorious shape
        if (selector.StartsWith('{'))
            selector = JsonShapeToSelector(selector);

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

            // rectangle, usually rotated: e.g.
            // <svg><rect x="10" y="20" width="30" height="40"
            // transform="rotate(45 25 40)"></rect></svg>
            // output its bounding rectangle attributes
            case "rect":
                double rectX = ParseNumber(shape.Attribute("x")!.Value);
                double rectY = ParseNumber(shape.Attribute("y")!.Value);
                double rectW = ParseNumber(shape.Attribute("width")!.Value);
                double rectH = ParseNumber(shape.Attribute("height")!.Value);
                string? transform = shape.Attribute("transform")?.Value;

                (double rx1, double ry1, double rx2, double ry2) =
                    GetRectangleBoundingBox(rectX, rectY, rectW, rectH, transform);
                AddBoundingBoxAttrs(rx1, ry1, rx2, ry2, target);

                // replace svg
                target.Element(ChgcTeiItemComposer.SVG_NS + "svg")?.Remove();
                XElement svgRect = RectangleToSVG(rectX, rectY, rectW, rectH);
                if (transform != null)
                {
                    svgRect.Elements().First().SetAttributeValue(
                        "transform", transform);
                }
                target.Add(svgRect);
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
                            new XAttribute("d", shape.Attribute("d")!.Value))));
                break;
        }
    }
}
