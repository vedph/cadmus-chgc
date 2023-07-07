using Cadmus.Core;
using Cadmus.Export;
using System.Xml.Linq;
using System.Linq;
using System;
using Cadmus.Chgc.Parts;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

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
    /// The SVG namespace.
    /// </summary>
    public static readonly XNamespace SVG_NS = "http://www.w3.org/2000/svg ";

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

    /// <summary>
    /// Adds the TEI header to the specified root element.
    /// </summary>
    /// <param name="root">The root element.</param>
    /// <exception cref="ArgumentNullException">root</exception>
    protected void AddHeader(XElement root)
    {
        if (root is null) throw new ArgumentNullException(nameof(root));

        root.Add(new XElement(TEI_NS + "teiHeader",
            new XElement(TEI_NS + "fileDesc",
                new XElement(TEI_NS + "titleStmt",
                    new XElement(TEI_NS + "title", new XAttribute("type", "main"),
                        "Compendium Historiae in genealogia Christi"),
                    new XElement(TEI_NS + "title", new XAttribute("type", "sub"),
                        "Electronic transcription of the manuscript " +
                            CurrentGroupId),
                    new XElement(TEI_NS + "author",
                        "Petrus von Poitiers",
                        new XElement(TEI_NS + "ex", "Petrus Pictaviensis")),
                    new XElement(TEI_NS + "respStmt",
                        new XElement(TEI_NS + "resp", "edited by"),
                        new XElement(TEI_NS + "persName",
                            new XAttribute("ref", "#")
                    ))),
                new XElement(TEI_NS + "publicationStmt",
                    new XElement(TEI_NS + "publisher",
                        new XElement(TEI_NS + "orgName",
                            new XAttribute("corresp",
                                "https://kunstgeschichte.unigraz.at"),
                            "Institut für Kunstgeschichte, " +
                            "Karl-Franzens-Universität Graz")),
                    new XElement(TEI_NS + "authority",
                        new XElement(TEI_NS + "orgName",
                            new XAttribute("corresp",
                                "https://informationsmodellierung.unigraz.at"),
                            "Zentrum für Informationsmodellierung - Austrian " +
                            "Centre for Digital Humanities, " +
                            "Karl-Franzens-Universität Graz")),
                    new XElement(TEI_NS + "distributor",
                        new XElement(TEI_NS + "orgName",
                            new XAttribute("ref", "https://gams.uni-graz.at"),
                            "GAMS - Geisteswissenschaftliches Asset Management System")),
                    new XElement(TEI_NS + "availability",
                        new XElement(TEI_NS + "licence",
                            new XAttribute("target",
                                "https://creativecommons.org/licenses/by-ncsa/4.0"),
                            "Creative Commons BY-NC-SA 4.0")),
                    new XElement(TEI_NS + "date", DateTime.Now.Year),
                    new XElement(TEI_NS + "pubPlace", "Graz")),
                new XElement(TEI_NS + "sourceDesc",
                    new XElement(TEI_NS + "p")))));
    }

    /// <summary>
    /// Sets up <see cref="Output"/> and its document.
    /// </summary>
    /// <param name="doc">The document.</param>
    /// <exception cref="ArgumentNullException">doc</exception>
    protected void SetupOutput(XDocument doc)
    {
        if (doc is null) throw new ArgumentNullException(nameof(doc));

        // ensure root TEI element exists
        if (doc.Root == null) doc.Add(new XElement(TEI_NS + "TEI"));

        // ensure teiHeader exists
        if (doc.Root!.Element(TEI_NS + "teiHeader") == null)
            AddHeader(doc.Root);

        // ensure TEI/facsimile exists
        XElement? facsimile = doc.Root.Element(TEI_NS + "facsimile");
        if (facsimile == null)
        {
            facsimile = new(TEI_NS + "facsimile");
            doc.Root!.Add(facsimile);
        }
        Output!.Data[M_FACS_KEY] = facsimile;

        // ensure TEI/text exists
        XElement? text = doc.Root.Element(TEI_NS + "text");
        if (text == null)
        {
            text = new(TEI_NS + "text");
            doc.Root.Add(text);
        }

        // ensure TEI/text/body exists
        XElement? body = text.Element(TEI_NS + "body");
        if (body == null)
        {
            body = new(TEI_NS + "body");
            text.Add(body);
        }
        Output!.Data[M_BODY_KEY] = body;
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
    /// Builds the annotation IDs, adding numeric suffixes to distinguish
    /// different zones targeting the same entity ID.
    /// </summary>
    /// <param name="imageId">The image identifier.</param>
    /// <param name="annotations">The annotations.</param>
    /// <returns>List of IDs, one per received annotation, in the same order.
    /// It is assumed that the received annotations are already sorted by
    /// the default unsuffixed ID.</returns>
    private static IList<string> BuildAnnotationIds(string imageId,
        IList<ChgcImageAnnotation> annotations)
    {
        List<string> ids = new(annotations.Count);
        foreach (ChgcImageAnnotation ann in annotations)
            ids.Add(imageId + "/" + ann.Eid);

        int i = 0;
        while (i < ids.Count - 1)
        {
            if (ids[i + 1] == ids[i])
            {
                string id = ids[i];
                int j = i;
                int n = 1;
                while (j < ids.Count && ids[j] == id)
                    ids[j++] += $"-{n++:00}";
                i = j;
            }
            else i++;
        }

        return ids;
    }

    /// <summary>
    /// Inserts the specified element in order among the child elements of
    /// element parent.
    /// </summary>
    /// <param name="parent">The parent.</param>
    /// <param name="element">The element.</param>
    /// <param name="attrName">Name of the attribute to use as sort key.</param>
    /// <param name="childA">The optional child element before which the element
    /// cannot be inserted.</param>
    /// <param name="childB">The optional child element after which the element
    /// cannot be inserted.</param>
    /// <exception cref="ArgumentNullException">parent or element or attrName
    /// </exception>
    public static void InsertInOrder(XElement parent, XElement element,
        XName attrName, XElement? childA = null, XElement? childB = null)
    {
        if (parent is null) throw new ArgumentNullException(nameof(parent));
        if (element is null) throw new ArgumentNullException(nameof(element));
        if (attrName is null) throw new ArgumentNullException(nameof(attrName));

        string newA = element.Attribute(attrName)?.Value ?? "";

        foreach (XElement child in parent.Elements(element.Name))
        {
            // must be after A
            if (childA != null && !child.ElementsBeforeSelf().Contains(childA))
                continue;
            // must be before B
            if (childB != null && !child.ElementsAfterSelf().Contains(childB))
                break;

            string a = child.Attribute(attrName)?.Value ?? "";
            if (string.CompareOrdinal(newA, a) < 0)
            {
                child.AddBeforeSelf(element);
                return;
            }
        }
        if (childB != null) childB.AddBeforeSelf(element);
        else parent.Add(element);
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
            InsertInOrder(facs, surface, "n");
        }

        // body/pb n=ID source=item ID
        XElement? pb = body.Elements(TEI_NS + "pb").FirstOrDefault(
            e => e.Attribute("n")!.Value == imageId);
        if (pb == null)
        {
            pb = new XElement(TEI_NS + "pb",
                new XAttribute("n", imageId),
                new XAttribute("source", "#" + item.Id));
            InsertInOrder(body, pb, "n");
        }

        // part's annotations (sorted by ID)
        List<ChgcImageAnnotation> sortedAnnotations = (
            from a in part.Annotations
            let annId = imageId + "/" + a.Eid
            orderby annId
            select a).ToList();
        IList<string> annIds = BuildAnnotationIds(imageId, sortedAnnotations);

        for (int i = 0; i < sortedAnnotations.Count; i++)
        {
            ChgcImageAnnotation ann = sortedAnnotations[i];
            string annId = annIds[i];
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
                InsertInOrder(surface, zone, XML_NS + "id");
            }
            SelectorXmlConverter.Convert(ann.Selector, zone);

            // body/div according to type (after pb)
            string annIdRef = "#" + annId;
            XElement? div = body.Elements(TEI_NS + "div").FirstOrDefault(
                e => e.Attribute("facs")!.Value == annIdRef);
            if (div == null)
            {
                div = new(TEI_NS + "div", new XAttribute("source", ann.Id));
                XElement? nextPb = pb.ElementsAfterSelf(TEI_NS + "pb")
                    .FirstOrDefault();
                InsertInOrder(body, div, "facs", pb, nextPb);
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
