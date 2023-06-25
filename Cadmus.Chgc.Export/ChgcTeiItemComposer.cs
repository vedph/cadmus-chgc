using Cadmus.Core;
using Cadmus.Export;
using System.Xml.Linq;
using System.Linq;
using System;
using Cadmus.Chgc.Parts;

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
    public readonly XNamespace XML_NS = "http://www.w3.org/XML/1998/namespace";

    /// <summary>
    /// The TEI namespace.
    /// </summary>
    public readonly XNamespace TEI_NS = "http://www.tei-c.org/ns/1.0";

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
        XElement? facs = Context.GetData(M_FACS_KEY) as XElement;
        if (facs == null)
            throw new InvalidOperationException("Expected facsimile element");

        // facsimile (set by derived class)
        XElement? body = Context.GetData(M_BODY_KEY) as XElement;
        if (body == null)
            throw new InvalidOperationException("Expected body element");

        // facsimile/surface n=ID
        // TODO id
        string imageId = $"{CurrentGroupId}/" + part.Annotations[0].Target!.Id;
        XElement surface = new(TEI_NS + "surface",
            new XAttribute("n", imageId));
        facs.Add(surface);

        foreach (ChgcImageAnnotation ann in part.Annotations)
        {
            string annId = imageId + "/" + ann.Eid;

            // facsimile/surface/zone
            XElement zone;
            surface.Add(zone = new XElement(TEI_NS + "zone",
                new XAttribute(XML_NS + "id", annId)));
            SelectorXmlConverter.Convert(ann.Selector, zone);

            // body/pb n=ID
            body.Add(new XElement(TEI_NS + "pb",
                new XAttribute("n", imageId)));

            // body/div according to type
            XElement div = new(TEI_NS + "div");
            body.Add(div);

            // TODO content of div according to ann EID prefix
        }
    }
}
