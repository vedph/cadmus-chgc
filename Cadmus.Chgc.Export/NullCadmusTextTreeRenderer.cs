using Cadmus.Export;
using Fusi.Tools.Data;
using Microsoft.Extensions.Logging;
using Proteus.Core.Text;
using Proteus.Rendering;
using System.Collections.Generic;

namespace Cadmus.Chgc.Export;

/// <summary>
/// A no-op text tree renderer. CHGC TEI composers do not render text trees,
/// but the base <see cref="ItemComposer"/> requires a renderer to be set.
/// </summary>
public sealed class NullCadmusTextTreeRenderer : ICadmusTextTreeRenderer
{
    /// <summary>
    /// Gets the filters (always empty).
    /// </summary>
    public List<ITextFilter> Filters { get; } = [];

    /// <summary>
    /// Gets or sets the logger.
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void Reset(IRendererContext context)
    {
    }

    /// <summary>
    /// Renders nothing.
    /// </summary>
    public string? RenderHead(IRendererContext context) => null;

    /// <summary>
    /// Renders nothing.
    /// </summary>
    public string? Render(TreeNode<ExportedSegment> tree,
        IRendererContext context) => null;

    /// <summary>
    /// Renders nothing.
    /// </summary>
    public string? RenderTail(IRendererContext context) => null;
}
