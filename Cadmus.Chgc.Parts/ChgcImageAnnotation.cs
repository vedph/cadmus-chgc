using Cadmus.Img.Parts;

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
    /// Gets or sets an optional label.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets a generic note.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChgcImageAnnotation"/> class.
    /// </summary>
    public ChgcImageAnnotation()
    {
        Eid = "";
    }
}
