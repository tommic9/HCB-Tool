using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Infrastructure.WithoutOpen;

namespace HCB.RevitAddin.Features.UpgradeAndCopy;

[Transaction(TransactionMode.Manual)]
public sealed class UpgradeAndCopyCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        WithoutOpenDialogService dialogService = new();
        IReadOnlyList<string> filePaths = dialogService.PickRevitFiles();
        if (filePaths.Count == 0)
        {
            return Result.Cancelled;
        }

        string? outputFolderPath = dialogService.PickFolderPath("Wybierz folder docelowy dla kopii po upgrade.");
        if (string.IsNullOrWhiteSpace(outputFolderPath))
        {
            return Result.Cancelled;
        }

        UpgradeAndCopyService service = new();
        var result = service.Process(commandData.Application.Application, filePaths, outputFolderPath);

        TaskDialog.Show(
            "WithoutOpen - Upgrade Copy",
            $"Pliki: {result.Entries.Count}\n" +
            $"Sukces: {result.SuccessCount}\n" +
            $"Pominiete: {result.SkippedCount}\n" +
            $"Bledy: {result.FailedCount}\n\n" +
            $"{string.Join("\n", result.Entries.Take(12).Select(entry => $"{Path.GetFileName(entry.FilePath)} | {entry.Status} | {entry.Message}"))}");

        return Result.Succeeded;
    }
}
