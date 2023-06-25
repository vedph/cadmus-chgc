using System;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Cadmus.Chgc.Export;

public static class SelectorXmlConverter
{
    public static void Convert(string selector, XElement target)
    {
        if (selector is null) throw new ArgumentNullException(nameof(selector));
        if (target is null) throw new ArgumentNullException(nameof(target));

        // rectangle
        if (selector.StartsWith("xywh=pixel:", StringComparison.Ordinal))
        {
            string csv = selector[11..];
            string[] values = csv.Split(',');
            if (values.Length != 4)
                throw new ArgumentException("Invalid selector: " + selector);
            target.Add(new XAttribute("ulx", values[0]));
            target.Add(new XAttribute("uly", values[1]));
            target.Add(new XAttribute("lrx", values[2]));
            target.Add(new XAttribute("lry", values[3]));
            return;
        }

        // polygon: <svg><polygon points="..."/> e.g.:
        // <svg><polygon points="269,389 246,467 368,529 439,413 372,379">
        // </polygon></svg>
        Match m = Regex.Match(selector,
            @"^<svg><polygon\s+points=""([^""]+)"""); 
        if (m.Success)
        {
            // TODO polygon
            return;
        }

        // circle: e.g.
        // <svg><circle cx=\"364.5\" cy=\"461\" r=\"141.2276530995258\"></circle></svg>

        // ellipse: e.g.
        // <svg><ellipse cx=\"115.5\" cy=\"506\" rx=\"37.5\" ry=\"72\"></ellipse></svg>

        // freehand: e.g.
        // <svg><path d=\"M381 44 L381 44 L381 45 L382 46 L382 47 L384 49..."></path></svg>
    }
}
