using Cadmus.Chgc.Cli.Services;
using Cadmus.Chgc.Import;
using Cadmus.Core.Storage;
using Cadmus.Index;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Cadmus.Chgc.Cli.Commands;

internal sealed class ImportTeiCommand : AsyncCommand<ImportTeiCommandOptions>
{
    public override Task<int> ExecuteAsync(CommandContext context,
               ImportTeiCommandOptions settings)
    {
        AnsiConsole.MarkupLine("[red]IMPORT TEI[/]");
        AnsiConsole.MarkupLine($"Input mask: [cyan]{settings.InputFileMask}[/]");
        if (!string.IsNullOrEmpty(settings.DatabaseName))
            AnsiConsole.MarkupLine($"Database: [cyan]{settings.DatabaseName}[/]");

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .Start("Importing TEI...", ctx =>
            {
                // repository
                AnsiConsole.MarkupLine("Creating data repository...");
                string cs = string.Format(
                    CliAppContext.Configuration.GetConnectionString("Default")!,
                    settings.DatabaseName);
                ICadmusRepository repository =
                    new ChgcRepositoryProvider(cs).CreateRepository();

                // index writer
                AnsiConsole.MarkupLine("Creating index writer...");
                string indexCS = string.Format(
                    CliAppContext.Configuration.GetConnectionString("Index")!,
                    settings.DatabaseName);
                StandardItemIndexFactoryProvider provider = new(indexCS);

                // load profile from current directory's Assets subfolder
                string profilePath = Path.Combine(
                    Directory.GetCurrentDirectory(), "Assets", "profile.json");
                string profile = File.ReadAllText(profilePath);

                IItemIndexWriter? indexWriter = provider.GetFactory(profile)
                    .GetItemIndexWriter() ??
                    throw new InvalidOperationException(
                        "Unable to instantiate index writer");

                // import
                string dir = Path.GetDirectoryName(settings.InputFileMask) ?? "";
                string pattern = Path.GetFileName(settings.InputFileMask) ?? "*.xml";
                ChgcItemImporter importer = new(repository, indexWriter)
                {
                    UriShortenerPattern =
                        string.IsNullOrEmpty(settings.UriShortenerPattern)
                        ? null : new Regex(settings.UriShortenerPattern),
                };
                int added = 0;

                foreach (string path in Directory.GetFiles(dir, pattern)
                    .OrderBy(s => s))
                {
                    AnsiConsole.MarkupLine($"Importing [yellow]{path}[/]...");
                    XDocument doc = XDocument.Load(path);
                    string groupId = Path.GetFileNameWithoutExtension(path);
                    added += importer.Import(groupId, doc);
                }

                AnsiConsole.MarkupLine("Items imported: [yellow]{0}[/]", added);
            });

        return Task.FromResult(0);
    }
}

public class ImportTeiCommandOptions : CommandSettings
{
    [CommandArgument(0, "<InputFileMask>")]
    [Description("The input file(s) mask")]
    public string? InputFileMask { get; set; }

    [CommandOption("-d|--database <DatabaseName>")]
    [Description("The database name (default=cadmus-chgc)")]
    [DefaultValue("cadmus-chgc")]
    public string? DatabaseName { get; set; }

    [CommandOption("-p|--pattern <UriShortenerPattern>")]
    [Description("The URI shortener pattern for item's description.")]
    public string? UriShortenerPattern { get; set; }
}