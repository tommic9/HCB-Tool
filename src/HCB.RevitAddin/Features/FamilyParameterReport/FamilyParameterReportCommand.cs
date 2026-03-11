using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Infrastructure.WithoutOpen;

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

        string? csvPath = dialogService.PickCsvOutputPath("withoutopen-family-parameters.csv");
        if (!string.IsNullOrWhiteSpace(csvPath))
        {
            service.ExportCsv(result.Rows, csvPath);
        }

        TaskDialog.Show(
            "WithoutOpen - Family Report",
            $"Rodziny zeskanowane: {result.Rows.Select(row => row.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).Count()}\n" +
            $"Parametry: {result.Rows.Count}\n" +
            $"Bledne pliki: {result.FailedFiles.Count}\n\n" +
            $"{string.Join("\n", result.Rows.Take(12).Select(row => $"{Path.GetFileName(row.FilePath)} | {row.ParameterName} | {(row.IsInstance ? "Instance" : "Type")}"))}");

        return Result.Succeeded;
    }
}

