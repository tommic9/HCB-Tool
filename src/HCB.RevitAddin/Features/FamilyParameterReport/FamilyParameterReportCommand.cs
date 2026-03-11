using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.FamilyParameterReport.Models;
using HCB.RevitAddin.Infrastructure.WithoutOpen;
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
            new() { Key = "IsShared", Header = "Shared" },
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
            $"Rodziny zeskanowane: {familyCount} | Parametry: {result.Rows.Count} | Bledne pliki: {result.FailedFiles.Count}";

        ReportPreviewWindow previewWindow = new(
            "WithoutOpen - Family Report",
            summary,
            columns,
            rows,
            "withoutopen-family-parameters.csv",
            outputPath => service.ExportCsv(result.Rows, outputPath));

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
            ["IsShared"] = row.IsShared ? "Tak" : "Nie",
            ["TypeCount"] = row.TypeCount.ToString(),
            ["GroupTypeId"] = row.GroupTypeId,
            ["SpecTypeId"] = row.SpecTypeId,
            ["Formula"] = row.Formula,
            ["FilePath"] = row.FilePath
        };
    }
}
