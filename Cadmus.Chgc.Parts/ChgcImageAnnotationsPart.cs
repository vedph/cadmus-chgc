using Cadmus.Core;
using Fusi.Tools.Configuration;
using System.Collections.Generic;
using System.Text;

namespace Cadmus.Chgc.Parts;

/// <summary>
/// CHGC image annotations part.
/// Tag: <c>it.vedph.chgc.image-annotations</c>.
/// </summary>
/// <seealso cref="PartBase" />
[Tag("it.vedph.chgc.image-annotations")]
public sealed class ChgcImageAnnotationsPart : PartBase
{
    /// <summary>
    /// Gets or sets the annotations.
    /// </summary>
    public List<ChgcImageAnnotation> Annotations { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChgcImageAnnotationsPart"/>
    /// class.
    /// </summary>
    public ChgcImageAnnotationsPart()
    {
        Annotations = new List<ChgcImageAnnotation>();
    }

    /// <summary>
    /// Get all the key=value pairs (pins) exposed by the implementor.
    /// </summary>
    /// <param name="item">The optional item. The item with its parts
    /// can optionally be passed to this method for those parts requiring
    /// to access further data.</param>
    /// <returns>The pins: <c>tot-count</c> and a collection of pins with
    /// these keys: ....</returns>
    public override IEnumerable<DataPin> GetDataPins(IItem? item = null)
    {
        DataPinBuilder builder = new(DataPinHelper.DefaultFilter);

        builder.Set("tot", Annotations?.Count ?? 0, false);

        if (Annotations?.Count > 0)
        {
            foreach (ChgcImageAnnotation annotation in Annotations)
            {
                builder.AddValue("id", annotation.Id);
                builder.AddValue("eid", annotation.Eid);
                builder.AddValue("label", annotation.Label);
            }
        }

        return builder.Build(this);
    }

    /// <summary>
    /// Gets the definitions of data pins used by the implementor.
    /// </summary>
    /// <returns>Data pins definitions.</returns>
    public override IList<DataPinDefinition> GetDataPinDefinitions()
    {
        return new List<DataPinDefinition>(new[]
        {
            new DataPinDefinition(DataPinValueType.Integer,
               "tot-count",
               "The total count of annotations."),
            new DataPinDefinition(DataPinValueType.String,
                "target-id", "The target ID(s) of the annotations images."),
            new DataPinDefinition(DataPinValueType.String,
                "eid", "The EID(s) assigned to annotations."),
            new DataPinDefinition(DataPinValueType.String,
                "label", "The label(s) assigned to annotations.")
        });
    }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        StringBuilder sb = new();

        sb.Append("[ChcgImageAnnotationsPart]");

        if (Annotations?.Count > 0)
        {
            sb.Append(' ');
            int n = 0;
            foreach (var entry in Annotations)
            {
                if (++n > 3) break;
                if (n > 1) sb.Append("; ");
                sb.Append(entry);
            }
            if (Annotations.Count > 3)
                sb.Append("...(").Append(Annotations.Count).Append(')');
        }

        return sb.ToString();
    }
}
