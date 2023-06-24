using Cadmus.Core;
using Cadmus.Export;
using Fusi.Tools.Configuration;
using System;
using System.IO;
using System.Xml.Linq;

namespace Cadmus.Chgc.Export;

/// <summary>
/// File system based CHGC TEI item composer. This provides the infrastructure
/// for generating one TEI XML document per items group, which corresponds to
/// a manuscript.
/// </summary>
/// <seealso cref="ChgcTeiItemComposer" />
/// <seealso cref="IItemComposer" />
[Tag("it.vedph.item-composer.chgc.tei.fs")]
public sealed class FSChgcTeiItemComposer : ChgcTeiItemComposer, IItemComposer,
    IConfigurable<FSChgcTeiItemComposerOptions>
{
    private FSChgcTeiItemComposerOptions? _options;
    private XDocument? _doc;

    /// <summary>
    /// Configures the object with the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <exception cref="ArgumentNullException">options</exception>
    public void Configure(FSChgcTeiItemComposerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Not used.
    /// </summary>
    /// <param name="key">The writer's key.</param>
    /// <exception cref="ArgumentNullException">key</exception>
    protected override void EnsureWriter(string key)
    {
        // not used
    }

    private void OpenDocument()
    {
        _doc = new XDocument(new XElement(TEI_NS + "TEI"));

        // TODO add header content
        _doc.Root!.Add(new XElement(TEI_NS + "teiHeader",
            new XElement("fileDesc",
                new XElement("titleStmt",
                    new XElement("title", new XAttribute("type", "main"),
                        "Compendium Historiae in genealogia Christi"),
                    new XElement("title", new XAttribute("type", "sub"),
                        "Electronic transcription of the manuscript " +
                            CurrentGroupId),
                    new XElement("author",
                        "Petrus von Poitiers",
                        new XElement("ex", "Petrus Pictaviensis")),
                    new XElement("respStmt",
                        new XElement("resp", "edited by"))
                    ))));

        XElement facsimile = new(TEI_NS + "facsimile");
        _doc.Root.Add(facsimile);
        Output!.Data[M_FACS_KEY] = facsimile;

        XElement body = new(TEI_NS + "body");
        _doc.Root.Add(new XElement(TEI_NS + "text", body));
        Output!.Data[M_BODY_KEY] = body;
    }

    private void CloseDocument()
    {
        if (_doc == null) return;

        _doc.Save(Path.Combine(_options!.OutputDirectory ?? "",
            CurrentGroupId + ".xml"), SaveOptions.OmitDuplicateNamespaces);
    }

    /// <summary>
    /// Open the composer.
    /// </summary>
    /// <param name="output">The output object to use, or null to create
    /// a new one.</param>
    public override void Open(ItemComposition? output = null)
    {
        base.Open(output);
        OpenDocument();
    }

    /// <summary>
    /// Close the composer.
    /// </summary>
    public override void Close()
    {
        base.Close();
        CloseDocument();
    }

    /// <summary>
    /// Invoked when the item's group changed since the last call to
    /// <see cref="M:Cadmus.Export.ItemComposer.Compose(Cadmus.Core.IItem)" />.
    /// This can be used when processing grouped items in order.
    /// </summary>
    /// <param name="item">The new item.</param>
    /// <param name="prevGroupId">The previous group identifier.</param>
    protected override void OnGroupChanged(IItem item, string? prevGroupId)
    {
        base.OnGroupChanged(item, prevGroupId);
        CloseDocument();
        OpenDocument();
    }
}

/// <summary>
/// Options for <see cref="FSChgcTeiItemComposer"/>."/>
/// </summary>
public class FSChgcTeiItemComposerOptions
{
    /// <summary>
    /// Gets or sets the output directory.
    /// </summary>
    public string OutputDirectory { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FSChgcTeiItemComposerOptions"/>
    /// class.
    /// </summary>
    public FSChgcTeiItemComposerOptions()
    {
        OutputDirectory = "";
    }
}

