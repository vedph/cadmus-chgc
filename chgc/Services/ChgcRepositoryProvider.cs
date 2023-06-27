using Cadmus.Chgc.Parts;
using Cadmus.Core;
using Cadmus.Core.Config;
using Cadmus.Core.Storage;
using Cadmus.General.Parts;
using Cadmus.Mongo;
using System.Reflection;

namespace Cadmus.Chgc.Cli.Services;

internal class ChgcRepositoryProvider : IRepositoryProvider
{
    private readonly IPartTypeProvider _partTypeProvider;

    /// <summary>
    /// Gets or sets the connection string to use.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardRepositoryProvider"/>
    /// class.
    /// </summary>
    /// <exception cref="ArgumentNullException">configuration</exception>
    public ChgcRepositoryProvider(string connectionString)
    {
        ConnectionString = connectionString
            ?? throw new ArgumentNullException(nameof(connectionString));
        TagAttributeToTypeMap map = new();
        map.Add(new[]
        {
            // Cadmus.General.Parts
            typeof(NotePart).GetTypeInfo().Assembly,
            // Cadmus.Chgc.Parts
            typeof(ChgcImageAnnotationsPart).GetTypeInfo().Assembly,
        });

        _partTypeProvider = new StandardPartTypeProvider(map);
    }

    /// <summary>
    /// Gets the part type provider.
    /// </summary>
    /// <returns>part type provider</returns>
    public IPartTypeProvider GetPartTypeProvider()
    {
        return _partTypeProvider;
    }

    /// <summary>
    /// Creates a Cadmus repository.
    /// </summary>
    /// <returns>repository</returns>
    /// <exception cref="ArgumentNullException">null database</exception>
    public ICadmusRepository CreateRepository()
    {
        // create the repository (no need to use container here)
        MongoCadmusRepository repository =
            new(_partTypeProvider, new StandardItemSortKeyBuilder());

        repository.Configure(new MongoCadmusRepositoryOptions
        {
            ConnectionString = ConnectionString
        });

        return repository;
    }

}
