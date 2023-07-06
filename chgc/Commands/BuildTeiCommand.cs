using Cadmus.Chgc.Cli.Services;
using Cadmus.Chgc.Export;
using Cadmus.Core;
using Cadmus.Core.Storage;
using Cadmus.Export;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Cadmus.Chgc.Cli.Commands;

internal sealed class BuildTeiCommand : AsyncCommand<BuildTeiCommandOptions>
{
    public override Task<int> ExecuteAsync(CommandContext context,
        BuildTeiCommandOptions settings)
    {
        AnsiConsole.MarkupLine("[green]BUILD TEI[/]");
        AnsiConsole.MarkupLine($"Output dir: [cyan]{settings.OutputDirectory}[/]");
        if (!string.IsNullOrEmpty(settings.DatabaseName))
            AnsiConsole.MarkupLine($"Database: [cyan]{settings.DatabaseName}[/]");
        if (!string.IsNullOrEmpty(settings.GroupId))
            AnsiConsole.MarkupLine($"Group ID: [cyan]{settings.GroupId}[/]");

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .Start("Building TEI...", ctx =>
        {
            // collector
            AnsiConsole.MarkupLine("Creating collector...");
            string cs = string.Format(
                CliAppContext.Configuration.GetConnectionString("Default")!,
                settings.DatabaseName);

            MongoItemIdCollector collector = new();
            collector.Configure(new MongoItemIdCollectorOptions
            {
                ConnectionString = cs,
                GroupId = settings.GroupId,
            });

            // repository
            AnsiConsole.MarkupLine("Creating repository...");
            ICadmusRepository repository =
                new ChgcRepositoryProvider(cs).CreateRepository();

            // composer
            AnsiConsole.MarkupLine("Creating composer...");
            FSChgcTeiItemComposer composer = new();
            composer.Configure(new FSChgcTeiItemComposerOptions
            {
                OutputDirectory = settings.OutputDirectory
            });
            if (!Directory.Exists(settings.OutputDirectory))
                Directory.CreateDirectory(settings.OutputDirectory);

            composer.Open();
            foreach (string id in collector.GetIds())
            {
                IItem? item = repository.GetItem(id);
                if (item == null) continue;

                AnsiConsole.MarkupLineInterpolated(
                    $"{item.GroupId}: {item.Title} ({item.Id})");
                composer.Compose(item);
            }
            composer.Close();

            ctx.Refresh();
        });

        return Task.FromResult(0);
    }
}

public class BuildTeiCommandOptions : CommandSettings
{
    [CommandArgument(0, "<OutputDirectory>")]
    [Description("The output directory")]
    public string OutputDirectory { get; set; }

    [CommandOption("-d|--database <DatabaseName>")]
    [Description("The database name (default=cadmus-chgc)")]
    [DefaultValue("cadmus-chgc")]
    public string? DatabaseName { get; set; }

    [CommandOption("-g|--group <GroupId>")]
    [Description("The ID of the group to limit export to")]
    public string? GroupId { get; set; }

    public BuildTeiCommandOptions()
    {
        OutputDirectory = "";
    }
}
