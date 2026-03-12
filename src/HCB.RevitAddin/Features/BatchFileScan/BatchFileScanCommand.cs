using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Infrastructure.WithoutOpen;
using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.BatchFileScan;

[Transaction(TransactionMode.Manual)]
public sealed class BatchFileScanCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        WithoutOpenDialogService dialogService = new();
        IReadOnlyList<string> filePaths = dialogService.PickRevitFiles();
        if (filePaths.Count == 0)
        {
            return Result.Cancelled;
        }

        BatchFileScanService service = new();
        var result = service.Scan(filePaths);

        int projects = result.Items.Count(item => item.FileKind == WithoutOpenFileKind.Project);
        int families = result.Items.Count(item => item.FileKind == WithoutOpenFileKind.Family);
        int cloud = result.Items.Count(item => item.IsCloudPath);
        int transmissionEligible = result.Items.Count(item => item.CanUseTransmissionData);
        int backgroundOpenEligible = result.Items.Count(item => item.CanOpenInBackground);

        List<ReportPreviewColumn> columns =
        [
            new() { Key = "FileName", Header = "Plik" },
            new() { Key = "FileKind", Header = "Typ" },
            new() { Key = "Format", Header = "Format" },
            new() { Key = "Status", Header = "Status" },
            new() { Key = "StatusMessage", Header = "Komunikat" },
            new() { Key = "IsCloudPath", Header = "Cloud" },
            new() { Key = "CanUseTransmissionData", Header = "TransmissionData" },
            new() { Key = "CanOpenInBackground", Header = "BackgroundOpen" },
            new() { Key = "FilePath", Header = "Sciezka" }
        ];

        IReadOnlyList<IReadOnlyDictionary<string, string>> rows = result.Items
            .Select(ToRow)
            .ToList();

        string summary =
            $"Pliki: {result.Items.Count} | Projekty: {projects} | Rodziny: {families} | " +
            $"Sukces: {result.SuccessCount} | Pominiete: {result.SkippedCount} | Bledy: {result.FailedCount} | " +
            $"Cloud: {cloud} | TransmissionData OK: {transmissionEligible} | Background Open OK: {backgroundOpenEligible}";

        ReportPreviewWindow previewWindow = new(
            "WithoutOpen - Scan Files",
            summary,
            columns,
            rows,
            "withoutopen-scan.csv",
            outputPath =>
            {
                WithoutOpenBatchLogService logService = new();
                logService.ExportScanToCsv(result.Items, outputPath);
            });

        previewWindow.ShowDialog();
        return Result.Succeeded;
    }

    private static IReadOnlyDictionary<string, string> ToRow(WithoutOpenFileScanItem item)
    {
        return new Dictionary<string, string>
        {
            ["FileName"] = item.FileName,
            ["FileKind"] = item.FileKind.ToString(),
            ["Format"] = item.Format,
            ["Status"] = item.Status.ToString(),
            ["StatusMessage"] = item.StatusMessage,
            ["IsCloudPath"] = item.IsCloudPath ? "Tak" : "Nie",
            ["CanUseTransmissionData"] = item.CanUseTransmissionData ? "Tak" : "Nie",
            ["CanOpenInBackground"] = item.CanOpenInBackground ? "Tak" : "Nie",
            ["FilePath"] = item.FilePath
        };
    }
}
