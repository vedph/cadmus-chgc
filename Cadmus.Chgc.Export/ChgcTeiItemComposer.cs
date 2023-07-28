using Cadmus.Core;
using Cadmus.Export;
using System.Xml.Linq;
using System.Linq;
using System;
using Cadmus.Chgc.Parts;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

    private static void AppendAttributeId(XElement target, XName attrName,
        string attrValue)
    {
        XAttribute? a = target.Attribute(attrName);
        if (a != null)
        {
            HashSet<string> ids = new(a.Value.Split((char[]?)null,
                StringSplitOptions.RemoveEmptyEntries))
            {
                attrValue
            };
            target.SetAttributeValue(attrName,
                string.Join(" ", ids.OrderBy(s => s)));
        }
        else target.SetAttributeValue(attrName, attrValue);
    }

    private static void BuildBodyEntryOutput(string id,
        string annId, string type, ChgcImageAnnotation ann, XElement target,
        bool append = false)
    {
        target.SetAttributeValue("type", type);

        if (append) AppendAttributeId(target, XML_NS + "id", id);
        else target.SetAttributeValue(XML_NS + "id", id);

        if (append) AppendAttributeId(target, "corresp", $"#{ann.Eid}");
        else target.SetAttributeValue("corresp", $"#{ann.Eid}");

        if (append) AppendAttributeId(target, "facs", $"#{annId}");
        else target.SetAttributeValue("facs", $"#{annId}");

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

    private static bool TextContainsId(string text, string id)
    {
        // return true if any of the tokens got by splitting text
        // at whitespaces is equal to id
        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Contains(id);
    }

    private static bool CompareIdsWithoutSuffix(string a, string b)
    {
        // return true if a and b are equal up to the last -NN
        Regex r = new(@"(.+)-\d+$", RegexOptions.Compiled);
        Match ma = r.Match(a);
        Match mb = r.Match(b);
        return !ma.Success && !mb.Success ?
            a == b : ma.Groups[1].Value == mb.Groups[1].Value;
    }

    private static bool TextContainsUnsuffixedId(string text, string id)
    {
        // return true if any of the tokens got by splitting text
        // at whitespaces and removing from each ID its final -NN suffix is equal
        // to id
        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Any(s => CompareIdsWithoutSuffix(s, id));
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
        if (part == null || part.Image == null || part.Annotations.Count == 0)
            return;

        // get facsimile (set by derived class)
        XElement? facs = Output!.GetData(M_FACS_KEY) as XElement
            ?? throw new InvalidOperationException("Expected facsimile element");

        // get body (set by derived class)
        XElement? body = Output!.GetData(M_BODY_KEY) as XElement
            ?? throw new InvalidOperationException("Expected body element");

        // (a) surface: image has a corresponding facsimile/surface with:
        // - @id = # + item GUID
        // - @n = friendly ID
        // - @source = image URI
        string friendlyImageId = $"{CurrentGroupId}/" +
            part.Annotations[0].Target!.Id;
        string itemId = "#" + item.Id;

        // reuse surface if exists, else create it
        XElement? surface = facs.Elements(TEI_NS + "surface").FirstOrDefault(
            e => e.Attribute("id")!.Value == itemId);
        if (surface == null)
        {
            surface = new(TEI_NS + "surface",
                new XAttribute(XML_NS + "id", itemId),
                new XAttribute("n", friendlyImageId),
                new XAttribute("source", part.Image.Uri));
            InsertInOrder(facs, surface, "n");
        }

        // (b) pb: body/pb with:
        // - @id = # + item GUID
        // - @n = friendly ID
        // reuse pb if exists, else create it
        XElement? pb = body.Elements(TEI_NS + "pb").FirstOrDefault(
            e => e.Attribute(XML_NS + "id")!.Value == itemId);
        if (pb == null)
        {
            pb = new XElement(TEI_NS + "pb",
                new XAttribute(XML_NS + "id", itemId),
                new XAttribute("n", friendlyImageId));
            InsertInOrder(body, pb, "n");
        }

        // (c) part's annotations (sorted by ID): zone's and div's
        List<ChgcImageAnnotation> sortedAnnotations = (
            from a in part.Annotations
            let annId = friendlyImageId + "/" + a.Eid
            orderby annId
            select a).ToList();
        IList<string> annIds = BuildAnnotationIds(friendlyImageId,
            sortedAnnotations);

        // usually there is 1 div per zone, but when two or more zones share
        // their entity ID, there is only 1 div for all zones with that ID.
        // as we are sorting by ID, we use prevDiv to reuse the preceding div
        // in this case
        XElement? prevDiv = null;

        for (int i = 0; i < sortedAnnotations.Count; i++)
        {
            ChgcImageAnnotation ann = sortedAnnotations[i];
            string annId = annIds[i];
            Logger?.LogInformation("Annotation {annId} {annEid} {annTarget}",
                annId, ann.Eid, ann.Target);

            // (c1) facsimile/surface/zone with:
            // - @id = annotation GUID
            // - @n = annID
            // reuse zone if exists, else create it
            XElement? zone = surface.Elements(TEI_NS + "zone").FirstOrDefault(
                e => e.Attribute(XML_NS + "id")!.Value == ann.Id);
            if (zone == null)
            {
                zone = new(TEI_NS + "zone",
                    new XAttribute(XML_NS + "id", ann.Id),
                    new XAttribute("n", annId));
                InsertInOrder(surface, zone, TEI_NS + "n");
            }
            SelectorXmlConverter.Convert(ann.Selector, zone);

            // (c2) body/div according to type (after pb)
            bool divPending = false;
            bool divMerged = false;
            XElement? nextPb = null;
            XElement? div;

            // if there is a previous div and it contains an ID equal to annId
            // except for their suffixes, reuse it adding the IDs to its attrs
            if (prevDiv != null && TextContainsUnsuffixedId(
                prevDiv.Attribute("facs")!.Value, "#" + annId))
            {
                div = prevDiv;
                divMerged = true;
            }
            else
            {
                div = body.Elements(TEI_NS + "div").FirstOrDefault(
                    e => TextContainsId(e.Attribute(XML_NS + "id")!.Value, ann.Id));
                if (div == null)
                {
                    div = new(TEI_NS + "div",
                        new XAttribute(XML_NS + "id", ann.Id));
                    nextPb = pb.ElementsAfterSelf(TEI_NS + "pb").FirstOrDefault();
                    divPending = true;
                }
            }
            prevDiv = div;

            switch (ann.Eid[0])
            {
                case 'n':
                    // node
                    BuildBodyEntryOutput(ann.Id, annId, "node", ann, div,
                        divMerged);
                    break;
                case 't':
                    // text
                    BuildBodyEntryOutput(ann.Id, annId, "text", ann, div,
                        divMerged);
                    break;
                case 'd':
                    // diagram
                    BuildBodyEntryOutput(ann.Id, annId, "diagram", ann, div,
                        divMerged);
                    break;
                case 'p':
                    // picture
                    BuildBodyEntryOutput(ann.Id, annId, "picture", ann, div,
                        divMerged);
                    break;
                case 'g':
                    // group
                    BuildBodyEntryOutput(ann.Id, annId, "group", ann, div,
                        divMerged);
                    break;
                case 'c':
                    // connection
                    BuildBodyEntryOutput(ann.Id, annId, "connection", ann, div,
                        divMerged);
                    break;
                default:
                    Logger?.LogWarning("Unknown annotation type in ID \"{type}\"",
                        ann.Eid);
                    break;
            }

            if (divPending) InsertInOrder(body, div, "corresp", pb, nextPb);
        }
    }
}
