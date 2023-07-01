using Cadmus.Export;
using Fusi.Tools.Configuration;
using System;
using System.Xml.Linq;

namespace Cadmus.Chgc.Export;

/// <summary>
/// RAM-based CHGC TEI item composer. This just saves its data in a RAM
/// XDocument specified in the output object.
/// </summary>
/// <seealso cref="ChgcTeiItemComposer" />
/// <seealso cref="IItemComposer" />
/// <seealso cref="IConfigurable&lt;RamChgcTeiItemComposerOptions&gt;" />
[Tag("it.vedph.item-composer.chgc.tei.ram")]
public sealed class RamChgcTeiItemComposer : ChgcTeiItemComposer, IItemComposer
{
    protected override void EnsureWriter(string key)
    {
        // nope
    }

    /// <summary>
    /// Open the composer. This implementation either uses the document received
    /// in <paramref name="output"/>, or creates a new one in it.
    /// </summary>
    /// <param name="output">The output object to use, or null to create
    /// a new one.</param>
    /// <exception cref="ArgumentNullException">output</exception>
    public override void Open(ItemComposition? output = null)
    {
        // users must explicitly pass an output object
        DocItemComposition o = output as DocItemComposition ??
            throw new ArgumentNullException(nameof(output));

        base.Open(o);
        o.Document ??= new XDocument(new XElement(TEI_NS + "TEI"));

        SetupOutput(o.Document);
    }
}

/// <summary>
/// Item composition result used by <see cref="RamChgcTeiItemComposer"/>.
/// </summary>
/// <seealso cref="ItemComposition" />
public class DocItemComposition : ItemComposition
{
    /// <summary>
    /// Gets or sets the target document. If set, the composer will patch it,
    /// otherwise it will create a new one.
    /// </summary>
    public XDocument? Document { get; set; }
}
