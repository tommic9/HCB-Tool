using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.FamilyParameterReport.Models;
using HCB.RevitAddin.Features.RenameFamilyContent;
using HCB.RevitAddin.Features.RenameFamilyContent.Models;
using HCB.RevitAddin.Features.RenameFamilyContent.UI;
using HCB.RevitAddin.Infrastructure.WithoutOpen;
using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.FamilyParameterReport;

[Transaction(TransactionMode.Manual)]
public sealed class FamilyParameterReportCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        WithoutOpenDialogService dialogService = new();
        IReadOnlyList<string> filePaths = dialogService.PickRevitFiles();
        if (filePaths.Count == 0)
        {
            return Result.Cancelled;
        }

        FamilyParameterReportService service = new();
        var result = service.BuildReport(commandData.Application.Application, filePaths);
        int familyCount = result.Rows.Select(row => row.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).Count();

        List<ReportPreviewColumn> columns =
        [
            new() { Key = "FileName", Header = "Plik" },
            new() { Key = "FamilyName", Header = "Rodzina" },
            new() { Key = "ParameterName", Header = "Parametr" },
            new() { Key = "ParameterKind", Header = "Instance/Type" },
            new() { Key = "ParameterSource", Header = "Zrodlo" },
            new() { Key = "IsShared", Header = "Shared" },
            new() { Key = "CanRename", Header = "Mozna zmienic" },
            new() { Key = "TypeCount", Header = "Liczba typow" },
            new() { Key = "GroupTypeId", Header = "Grupa" },
            new() { Key = "SpecTypeId", Header = "SpecTypeId" },
            new() { Key = "Formula", Header = "Formula" },
            new() { Key = "FilePath", Header = "Sciezka" }
        ];

        IReadOnlyList<IReadOnlyDictionary<string, string>> rows = result.Rows
            .Select(ToRow)
            .ToList();

        string summary =
            $"Rodziny zeskanowane: {familyCount} | Parametry: {result.Rows.Count} | Bledne pliki: {result.FailedFiles.Count} | Filtruj po rodzinie, nazwie parametru albo wartosciach typu Shared/Mozna zmienic.";

        ReportPreviewWindow previewWindow = new(
            "WithoutOpen - Family Report",
            summary,
            columns,
            rows,
            "withoutopen-family-parameters.csv",
            outputPath => service.ExportCsv(result.Rows, outputPath),
            "Zmien nazwe",
            row => RenameFromReportRow(commandData, row));

        previewWindow.ShowDialog();
        return Result.Succeeded;
    }

    private static IReadOnlyDictionary<string, string> ToRow(FamilyParameterReportRow row)
    {
        return new Dictionary<string, string>
        {
            ["FileName"] = Path.GetFileName(row.FilePath),
            ["FamilyName"] = row.FamilyName,
            ["ParameterName"] = row.ParameterName,
            ["ParameterKind"] = row.IsInstance ? "Instance" : "Type",
            ["ParameterSource"] = row.ParameterSource,
            ["IsShared"] = row.IsShared ? "Tak" : "Nie",
            ["CanRename"] = row.CanRename ? "Tak" : "Nie",
            ["TypeCount"] = row.TypeCount.ToString(),
            ["GroupTypeId"] = row.GroupTypeId,
            ["SpecTypeId"] = row.SpecTypeId,
            ["Formula"] = row.Formula,
            ["FilePath"] = row.FilePath
        };
    }

    private static void RenameFromReportRow(ExternalCommandData commandData, IReadOnlyDictionary<string, string> row)
    {
        string parameterName = row.TryGetValue("ParameterName", out string? parameterValue) ? parameterValue ?? string.Empty : string.Empty;
        string filePath = row.TryGetValue("FilePath", out string? pathValue) ? pathValue ?? string.Empty : string.Empty;
        bool canRename = row.TryGetValue("CanRename", out string? canRenameValue) && string.Equals(canRenameValue, "Tak", StringComparison.OrdinalIgnoreCase);

        if (!canRename)
        {
            TaskDialog.Show("WithoutOpen - Family Report", $"Parametr '{parameterName}' jest chroniony i nie powinien byc zmieniany tym narzedziem.");
            return;
        }

        RenameFamilyContentWindow window = new(
            new RenameFamilyContentOptions
            {
                Find = parameterName,
                Replace = parameterName,
                SaveAsCopy = true
            },
            $"Przygotowano rename dla parametru '{parameterName}'. Zmien pole 'Zamien na' albo ustaw dodatkowy prefiks/sufiks.");

        if (window.ShowDialog() != true)
        {
            return;
        }

        RenameFamilyContentService service = new();
        RenameFamilyContentResult result = service.Rename(commandData.Application.Application, [filePath], window.Options);
        WithoutOpenOperationLogEntry entry = result.Entries.FirstOrDefault() ?? new WithoutOpenOperationLogEntry
        {
            FilePath = filePath,
            Status = Infrastructure.WithoutOpen.Models.WithoutOpenOperationStatus.Skipped,
            Message = "Brak wyniku operacji rename."
        };

        TaskDialog.Show(
            "WithoutOpen - Rename Family Parameters",
            $"Plik: {Path.GetFileName(filePath)}\nStatus: {entry.Status}\nKomunikat: {entry.Message}");
    }
}

