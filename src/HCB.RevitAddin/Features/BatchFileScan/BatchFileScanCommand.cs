using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Infrastructure.WithoutOpen;

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
        string? csvPath = dialogService.PickCsvOutputPath("withoutopen-scan.csv");
        if (!string.IsNullOrWhiteSpace(csvPath))
        {
            WithoutOpenBatchLogService logService = new();
            logService.ExportScanToCsv(result.Items, csvPath);
        }

        int projects = result.Items.Count(item => item.FileKind == Infrastructure.WithoutOpen.Models.WithoutOpenFileKind.Project);
        int families = result.Items.Count(item => item.FileKind == Infrastructure.WithoutOpen.Models.WithoutOpenFileKind.Family);
        int cloud = result.Items.Count(item => item.IsCloudPath);
        int transmissionEligible = result.Items.Count(item => item.CanUseTransmissionData);
        int backgroundOpenEligible = result.Items.Count(item => item.CanOpenInBackground);

        TaskDialog.Show(
            "WithoutOpen - Scan Files",
            $"Pliki: {result.Items.Count}\n" +
            $"Projekty: {projects}\n" +
            $"Rodziny: {families}\n" +
            $"Sukces: {result.SuccessCount}\n" +
            $"Pominiete: {result.SkippedCount}\n" +
            $"Bledy: {result.FailedCount}\n" +
            $"Cloud: {cloud}\n" +
            $"TransmissionData OK: {transmissionEligible}\n" +
            $"Background Open OK: {backgroundOpenEligible}\n\n" +
            $"{string.Join("\n", result.Items.Take(10).Select(item => $"{item.FileName} | {item.Format} | {item.Status} | {item.StatusMessage}"))}");

        return Result.Succeeded;
    }
}
