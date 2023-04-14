using Cadmus.Img.Parts;
using System.Collections.Generic;

namespace Cadmus.Chgc.Parts;

/// <summary>
/// An image annotation for CHGC, including an essential set of metadata.
/// </summary>
/// <seealso cref="GalleryImageAnnotation" />
public class ChgcImageAnnotation : GalleryImageAnnotation
{
    /// <summary>
    /// Gets or sets the CHGC entity identifier. This is derived from a list
    /// of preset CHGC identifiers.
    /// </summary>
    public string Eid { get; set; }

    /// <summary>
    /// Gets or sets the renditions.
    /// </summary>
    public List<string> Renditions { get; set; }

    /// <summary>
    /// Gets or sets the line count.
    /// </summary>
    public short LineCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance has any call signs.
    /// </summary>
    public bool HasCallSign { get; set; }

    /// <summary>
    /// Gets or sets a generic note.
    /// </summary>
    public string? Note { get; set; }

    public ChgcImageAnnotation()
    {
        Eid = "";
        Renditions = new List<string>();
    }
}
