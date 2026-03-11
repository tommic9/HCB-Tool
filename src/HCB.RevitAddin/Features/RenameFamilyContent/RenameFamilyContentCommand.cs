using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.RenameFamilyContent.UI;
using HCB.RevitAddin.Infrastructure.WithoutOpen;
using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.RenameFamilyContent;

[Transaction(TransactionMode.Manual)]
public sealed class RenameFamilyContentCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        WithoutOpenDialogService dialogService = new();
        IReadOnlyList<string> familyPaths = dialogService.PickRevitFiles();
        if (familyPaths.Count == 0)
        {
            return Result.Cancelled;
        }

        RenameFamilyContentWindow window = new();
        if (window.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        RenameFamilyContentService service = new();
        var result = service.Rename(commandData.Application.Application, familyPaths, window.Options);

        List<ReportPreviewColumn> columns =
        [
            new() { Key = "FileName", Header = "Plik" },
            new() { Key = "Status", Header = "Status" },
            new() { Key = "Message", Header = "Komunikat" },
            new() { Key = "Duration", Header = "Czas [s]" },
            new() { Key = "FilePath", Header = "Sciezka" }
        ];

        IReadOnlyList<IReadOnlyDictionary<string, string>> rows = result.Entries
            .Select(ToRow)
            .ToList();

        string summary =
            $"Pliki: {result.Entries.Count} | Sukces: {result.SuccessCount} | Pominiete: {result.SkippedCount} | Bledy: {result.FailedCount}";

        ReportPreviewWindow previewWindow = new(
            "WithoutOpen - Rename Family",
            summary,
            columns,
            rows,
            "withoutopen-rename-family.csv",
            outputPath =>
            {
                WithoutOpenBatchLogService logService = new();
                logService.ExportOperationsToCsv(result.Entries, outputPath);
            });

        previewWindow.ShowDialog();
        return Result.Succeeded;
    }

    private static IReadOnlyDictionary<string, string> ToRow(WithoutOpenOperationLogEntry entry)
    {
        return new Dictionary<string, string>
        {
            ["FileName"] = Path.GetFileName(entry.FilePath),
            ["Status"] = entry.Status.ToString(),
            ["Message"] = entry.Message,
            ["Duration"] = entry.Duration.TotalSeconds.ToString("0.###"),
            ["FilePath"] = entry.FilePath
        };
    }
}
