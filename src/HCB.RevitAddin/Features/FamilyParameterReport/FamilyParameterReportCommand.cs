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
            row => RenameFromReportRows(commandData, [row]),
            "Batch rename",
            selectedRows => RenameFromReportRows(commandData, selectedRows));

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

    private static void RenameFromReportRows(ExternalCommandData commandData, IReadOnlyList<IReadOnlyDictionary<string, string>> selectedRows)
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> renameableRows = selectedRows
            .Where(CanRename)
            .ToList();

        if (renameableRows.Count == 0)
        {
            TaskDialog.Show("WithoutOpen - Family Report", "Z zaznaczenia nie ma parametrow, ktore mozna zmienic tym narzedziem.");
            return;
        }

        int protectedCount = selectedRows.Count - renameableRows.Count;
        IReadOnlyList<string> familyPaths = renameableRows
            .Select(GetFilePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        IReadOnlyList<string> targetKeys = renameableRows
            .Select(row => RenameFamilyContentService.BuildTargetKey(GetFilePath(row), GetParameterName(row)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        IReadOnlyList<string> parameterNames = renameableRows
            .Select(GetParameterName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        RenameFamilyContentWindow window = new(
            new RenameFamilyContentOptions
            {
                Find = parameterNames.Count == 1 ? parameterNames[0] : string.Empty,
                Replace = parameterNames.Count == 1 ? parameterNames[0] : string.Empty,
                SaveAsCopy = true,
                TargetParameterKeys = targetKeys
            },
            BuildRenameStatusText(renameableRows.Count, familyPaths.Count, parameterNames, protectedCount));

        if (window.ShowDialog() != true)
        {
            return;
        }

        RenameFamilyContentOptions windowOptions = window.Options;
        RenameFamilyContentOptions options = new()
        {
            Prefix = windowOptions.Prefix,
            Find = windowOptions.Find,
            Replace = windowOptions.Replace,
            Suffix = windowOptions.Suffix,
            SaveAsCopy = windowOptions.SaveAsCopy,
            OutputFolderPath = windowOptions.OutputFolderPath,
            TargetParameterKeys = targetKeys
        };

        RenameFamilyContentService service = new();
        RenameFamilyContentResult result = service.Rename(commandData.Application.Application, familyPaths, options);
        ShowRenameResultPreview(result);
    }

    private static string BuildRenameStatusText(int selectedRowCount, int familyCount, IReadOnlyList<string> parameterNames, int protectedCount)
    {
        string scopeText = parameterNames.Count == 1
            ? $"Zakres: {selectedRowCount} wiersz(y), {familyCount} rodzin(y), parametr '{parameterNames[0]}'."
            : $"Zakres: {selectedRowCount} wiersz(y), {familyCount} rodzin(y), {parameterNames.Count} roznych parametrow.";

        if (protectedCount <= 0)
        {
            return scopeText + " Zmiany zostana zastosowane tylko do zaznaczonych parametrow mozliwych do zmiany nazwy.";
        }

        return scopeText + $" Pominieto z zaznaczenia parametry chronione: {protectedCount}. Zmiany zostana zastosowane tylko do zaznaczonych parametrow mozliwych do zmiany nazwy.";
    }

    private static void ShowRenameResultPreview(RenameFamilyContentResult result)
    {
        List<ReportPreviewColumn> columns =
        [
            new() { Key = "FileName", Header = "Plik" },
            new() { Key = "Status", Header = "Status" },
            new() { Key = "Message", Header = "Komunikat" },
            new() { Key = "OutputPath", Header = "Plik wynikowy" },
            new() { Key = "Duration", Header = "Czas [s]" },
            new() { Key = "FilePath", Header = "Sciezka" }
        ];

        IReadOnlyList<IReadOnlyDictionary<string, string>> rows = result.Entries
            .Select(ToOperationRow)
            .ToList();

        string summary =
            $"Pliki: {result.Entries.Count} | Sukces: {result.SuccessCount} | Pominiete: {result.SkippedCount} | Bledy: {result.FailedCount}";

        ReportPreviewWindow previewWindow = new(
            "WithoutOpen - Rename Family Parameters",
            summary,
            columns,
            rows,
            "withoutopen-rename-family-parameters.csv",
            outputPath =>
            {
                WithoutOpenBatchLogService logService = new();
                logService.ExportOperationsToCsv(result.Entries, outputPath);
            });

        previewWindow.ShowDialog();
    }

    private static IReadOnlyDictionary<string, string> ToOperationRow(WithoutOpenOperationLogEntry entry)
    {
        return new Dictionary<string, string>
        {
            ["FileName"] = Path.GetFileName(entry.FilePath),
            ["Status"] = entry.Status.ToString(),
            ["Message"] = entry.Message,
            ["OutputPath"] = entry.OutputPath,
            ["Duration"] = entry.Duration.TotalSeconds.ToString("0.###"),
            ["FilePath"] = entry.FilePath
        };
    }

    private static bool CanRename(IReadOnlyDictionary<string, string> row)
    {
        return row.TryGetValue("CanRename", out string? canRenameValue) &&
               string.Equals(canRenameValue, "Tak", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetFilePath(IReadOnlyDictionary<string, string> row)
    {
        return row.TryGetValue("FilePath", out string? pathValue)
            ? pathValue ?? string.Empty
            : string.Empty;
    }

    private static string GetParameterName(IReadOnlyDictionary<string, string> row)
    {
        return row.TryGetValue("ParameterName", out string? parameterValue)
            ? parameterValue ?? string.Empty
            : string.Empty;
    }
}
