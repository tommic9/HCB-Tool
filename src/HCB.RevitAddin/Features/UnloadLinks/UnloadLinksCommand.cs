using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Infrastructure.WithoutOpen;

namespace HCB.RevitAddin.Features.UnloadLinks;

[Transaction(TransactionMode.Manual)]
public sealed class UnloadLinksCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        WithoutOpenDialogService dialogService = new();
        IReadOnlyList<string> filePaths = dialogService.PickRevitFiles();
        if (filePaths.Count == 0)
        {
            return Result.Cancelled;
        }

        UnloadLinksService service = new();
        var result = service.UnloadAllLinks(filePaths);

        TaskDialog.Show(
            "WithoutOpen - Unload Links",
            $"Pliki: {result.Entries.Count}\n" +
            $"Sukces: {result.SuccessCount}\n" +
            $"Pominiete: {result.SkippedCount}\n" +
            $"Bledy: {result.FailedCount}\n\n" +
            $"{string.Join("\n", result.Entries.Take(12).Select(entry => $"{Path.GetFileName(entry.FilePath)} | {entry.Status} | {entry.Message}"))}");

        return Result.Succeeded;
    }
}
