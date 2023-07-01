using Cadmus.Core;
using Cadmus.Export;
using Fusi.Tools.Configuration;
using System;
using System.IO;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

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

    private string GetTeiFilePath(string groupId) =>
        Path.Combine(_options!.OutputDirectory ?? "", groupId + ".xml");

    private void OpenDocument()
    {
        if (CurrentGroupId == null) return;

        // load or create XML document
        Logger?.LogInformation("Opening document for {groupId}", CurrentGroupId);
        string path = GetTeiFilePath(CurrentGroupId);
        _doc = File.Exists(path)
            ? XDocument.Load(path,
                LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo)
            : new XDocument(new XElement(TEI_NS + "TEI"));

        SetupOutput(_doc);
    }

    private void CloseDocument()
    {
        if (_doc == null || CurrentGroupId == null) return;

        // save XML document
        string path = GetTeiFilePath(CurrentGroupId);
        Logger?.LogInformation("Saving {path}", path);
        _doc.Save(path, SaveOptions.OmitDuplicateNamespaces);
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
        Logger?.LogInformation("New group ID: {groupId}", item.GroupId);

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
