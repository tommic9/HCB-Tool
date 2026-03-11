using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.RenameFamilyContent.UI;
using HCB.RevitAddin.Infrastructure.WithoutOpen;

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

        TaskDialog.Show(
            "WithoutOpen - Rename Family",
            $"Pliki: {result.Entries.Count}\n" +
            $"Sukces: {result.SuccessCount}\n" +
            $"Pominiete: {result.SkippedCount}\n" +
            $"Bledy: {result.FailedCount}\n\n" +
            $"{string.Join("\n", result.Entries.Take(12).Select(entry => $"{Path.GetFileName(entry.FilePath)} | {entry.Status} | {entry.Message}"))}");

        return Result.Succeeded;
    }
}
