using Cadmus.Chgc.Parts;
using Cadmus.Core;
using Cadmus.Core.Storage;
using Cadmus.Img.Parts;
using Cadmus.Index;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Cadmus.Chgc.Import;

/// <summary>
/// CHGC item importer. This is used to import a set of pages (images) from
/// a TEI document, so that users can start annotating them without having
/// to create an item for each page.
/// </summary>
public class ChgcItemImporter
{
    private const string FACET_ID = "default";

    /// <summary>
    /// The XML namespace.
    /// </summary>
    public static readonly XNamespace XML_NS = "http://www.w3.org/XML/1998/namespace";

    /// <summary>
    /// The TEI namespace.
    /// </summary>
    public static readonly XNamespace TEI_NS = "http://www.tei-c.org/ns/1.0";

    private readonly ICadmusRepository _repository;
    private readonly IItemIndexWriter? _indexWriter;

    /// <summary>
    /// Gets or sets the regular expression pattern to use to shorten the URI
    /// for using it in the imported item's description. If not specified,
    /// the URI will be copied as is; else, it will be shortened by replacing
    /// it with the first group of the match.
    /// </summary>
    public Regex? UriShortenerPattern { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChgcItemImporter"/> class.
    /// </summary>
    /// <param name="repository">The repository for the target database.</param>
    /// <param name="indexWriter">The optional index writer.</param>
    /// <exception cref="ArgumentNullException">repository</exception>
    public ChgcItemImporter(ICadmusRepository repository,
        IItemIndexWriter? indexWriter)
    {
        _repository = repository ??
            throw new ArgumentNullException(nameof(repository));
        _indexWriter = indexWriter;
    }

    /// <summary>
    /// Imports the specified document for the manuscript identified by
    /// <paramref name="groupId"/>.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="doc">The TEI document.</param>
    /// <returns>Count of items added.</returns>
    /// <exception cref="ArgumentNullException">groupId or doc</exception>
    /// <exception cref="InvalidOperationException">Missing page number in
    /// surface {n}, or Missing source URI in surface {n}.</exception>
    public int Import(string groupId, XDocument doc)
    {
        if (groupId is null) throw new ArgumentNullException(nameof(groupId));
        if (doc is null) throw new ArgumentNullException(nameof(doc));

        XElement? facsimile = doc.Root?.Element(TEI_NS + "facsimile");
        if (facsimile == null) return 0;

        ItemFilter filter = new()
        {
            FacetId = FACET_ID,
            GroupId = groupId,
        };

        int n = 0, added = 0;
        foreach (XElement surface in facsimile.Elements(TEI_NS + "surface"))
        {
            n++;
            string? page = (surface.Attribute("n")?.Value) ??
                throw new InvalidOperationException(
                    $"Missing page number in surface {n}");
            string? uri = (surface.Attribute("source")?.Value) ??
                throw new InvalidOperationException(
                    $"Missing source URI in surface {n}");

            string shortenedUri = UriShortenerPattern == null
                ? uri
                : UriShortenerPattern.Replace(uri, "$1");

            IItem item = new Item
            {
                FacetId = FACET_ID,
                GroupId = groupId,
                Title = $"{groupId} {n:000} {page}",
                Description = $"{page}: {shortenedUri}".TrimEnd(),
                Flags = 1,  // = imported
                CreatorId = "zeus",
                UserId = "zeus",
            };

            filter.Title = item.Title;
            ItemInfo? old = _repository.GetItems(filter).Items.FirstOrDefault();
            if (old == null)
            {
                ChgcImageAnnotationsPart part = new()
                {
                    ItemId = item.Id,
                    CreatorId = "zeus",
                    UserId = "zeus",
                    Image = new GalleryImage
                    {
                        Id = $"{n}",
                        Uri = uri,
                        Title = $"{groupId}: {page}",
                        Description = page,
                    }
                };
                item.Parts.Add(part);

                _repository.AddItem(item);
                _repository.AddPart(part);

                // index if any
                if (_indexWriter != null)
                {
                    _indexWriter.WriteItem(item);
                    _indexWriter.WritePart(item, part);
                }

                added++;
            }
        }
        return added;
    }
}