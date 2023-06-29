using Cadmus.Core;
using Cadmus.Export;
using System.Xml.Linq;
using System.Linq;
using System;
using Cadmus.Chgc.Parts;
using Microsoft.Extensions.Logging;

namespace Cadmus.Chgc.Export;

/// <summary>
/// CHGC TEI item composer base class. The task of this class is to build
/// the XML elements representing the item's annotations, storing them in
/// output metadata. These are represented by two elements: one for facsimile,
/// and another for the body.
/// </summary>
/// <seealso cref="ItemComposer" />
public abstract class ChgcTeiItemComposer : ItemComposer
{
    /// <summary>
    /// The facsimile flow metadata key (<c>chgc-facs</c>).
    /// </summary>
    public const string M_FACS_KEY = "chgc-facs";

    /// <summary>
    /// The body flow metadata key (<c>chgc-body</c>).
    /// </summary>
    public const string M_BODY_KEY = "chgc-body";

    /// <summary>
    /// The XML namespace.
    /// </summary>
    public static readonly XNamespace XML_NS = "http://www.w3.org/XML/1998/namespace";

    /// <summary>
    /// The TEI namespace.
    /// </summary>
    public static readonly XNamespace TEI_NS = "http://www.tei-c.org/ns/1.0";

    /// <summary>
    /// Gets the current group identifier.
    /// </summary>
    public string? CurrentGroupId { get; private set; }

    /// <summary>
    /// Invoked when the item's group changed since the last call to
    /// <see cref="M:Cadmus.Export.ItemComposer.Compose(Cadmus.Core.IItem)" />.
    /// This can be used when processing grouped items in order.
    /// </summary>
    /// <param name="item">The new item.</param>
    /// <param name="prevGroupId">The previous group identifier.</param>
    protected override void OnGroupChanged(IItem item, string? prevGroupId)
    {
        CurrentGroupId = item.GroupId;
    }

    private static XElement BuildTextParagraphs(string text, XElement target)
    {
        foreach (string line in text.Split('\n'))
            target.Add(new XElement(TEI_NS + "p", line.Trim()));
        return target;
    }

    private static void BuildLabelAndText(ChgcImageAnnotation ann, XElement target)
    {
        // label
        XElement? label = target.Element(TEI_NS + "label");
        if (!string.IsNullOrEmpty(ann.Label))
        {
            if (label != null) label.Value = ann.Label;
            else target.Add(new XElement(TEI_NS + "label", ann.Label));
        }
        else label?.Remove();

        // text
        XElement? note = target.Element(TEI_NS + "text");
        if (!string.IsNullOrEmpty(ann.Note))
        {
            if (note != null) note.Value = ann.Note;
            else
            {
                target.Add(BuildTextParagraphs(ann.Note,
                    new XElement(TEI_NS + "text")));
            }
        }
        else note?.Remove();
    }

    private static void BuildBodyEntryOutput(string annId, string type,
        ChgcImageAnnotation ann, XElement target)
    {
        target.SetAttributeValue("type", type);
        target.SetAttributeValue("corresp", $"#{ann.Eid}");
        target.SetAttributeValue("facs", $"#{annId}");

        BuildLabelAndText(ann, target);
    }

    /// <summary>
    /// Does the composition for the specified item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <exception cref="ArgumentNullException">item</exception>
    /// <exception cref="InvalidOperationException">Expected facsimile
    /// element or expected body element.</exception>
    protected override void DoCompose(IItem item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));

        // get image annotations part
        ChgcImageAnnotationsPart? part = item.Parts
            .OfType<ChgcImageAnnotationsPart>().FirstOrDefault();
        if (part == null || part.Annotations.Count == 0) return;

        // facsimile (set by derived class)
        XElement? facs = Output!.GetData(M_FACS_KEY) as XElement
            ?? throw new InvalidOperationException("Expected facsimile element");

        // body (set by derived class)
        XElement? body = Output!.GetData(M_BODY_KEY) as XElement
            ?? throw new InvalidOperationException("Expected body element");

        // facsimile/surface @n=ID @source=item ID
        string imageId = $"{CurrentGroupId}/" + part.Annotations[0].Target!.Id;
        XElement? surface = facs.Elements(TEI_NS + "surface").FirstOrDefault(
            e => e.Attribute("n")!.Value == imageId);
        if (surface == null)
        {
            surface = new(TEI_NS + "surface",
                new XAttribute("n", imageId),
                new XAttribute("source", "#" + item.Id));
            facs.Add(surface);
        }

        // body/pb n=ID source=item ID
        body.Add(new XElement(TEI_NS + "pb",
            new XAttribute("n", imageId),
            new XAttribute("source", "#" + item.Id)));

        // part's annotations (sorted by ID)
        var sortedAnnotations = from a in part.Annotations
                         let annId = imageId + "/" + a.Eid
                         orderby annId
                         select a;
        foreach (ChgcImageAnnotation ann in sortedAnnotations)
        {
            string annId = imageId + "/" + ann.Eid;
            Logger?.LogInformation("Annotation {annId} {annEid} {annTarget}",
                annId, ann.Eid, ann.Target);

            // facsimile/surface/zone @id=annID @source=GUID
            XElement? zone = surface.Elements(TEI_NS + "zone").FirstOrDefault(
                e => e.Attribute(XML_NS + "id")!.Value == annId);
            if (zone == null)
            {
                zone = new(TEI_NS + "zone",
                    new XAttribute(XML_NS + "id", annId),
                    new XAttribute("source", ann.Id));
                surface.Add(zone);
            }
            SelectorXmlConverter.Convert(ann.Selector, zone);

            // body/div according to type
            string annIdRef = "#" + annId;
            XElement? div = body.Elements(TEI_NS + "div").FirstOrDefault(
                e => e.Attribute("facs")!.Value == annIdRef);
            if (div == null)
            {
                div = new(TEI_NS + "div", new XAttribute("source", ann.Id));
                body.Add(div);
            }

            switch (ann.Eid[0])
            {
                case 'n':
                    // node
                    BuildBodyEntryOutput(annId, "node", ann, div);
                    break;
                case 't':
                    // text
                    BuildBodyEntryOutput(annId, "text", ann, div);
                    break;
                case 'd':
                    // diagram
                    BuildBodyEntryOutput(annId, "diagram", ann, div);
                    break;
                case 'p':
                    // picture
                    BuildBodyEntryOutput(annId, "picture", ann, div);
                    break;
                case 'g':
                    // group
                    BuildBodyEntryOutput(annId, "group", ann, div);
                    break;
                case 'c':
                    // connection
                    BuildBodyEntryOutput(annId, "connection", ann, div);
                    break;
                default:
                    Logger?.LogWarning("Unknown annotation type in ID \"{type}\"",
                        ann.Eid);
                    break;
            }
        }
    }
}
