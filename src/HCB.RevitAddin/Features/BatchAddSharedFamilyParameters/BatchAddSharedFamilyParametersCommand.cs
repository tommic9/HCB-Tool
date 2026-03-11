using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.BatchAddSharedFamilyParameters.UI;
using HCB.RevitAddin.Infrastructure.WithoutOpen;

namespace HCB.RevitAddin.Features.BatchAddSharedFamilyParameters;

[Transaction(TransactionMode.Manual)]
public sealed class BatchAddSharedFamilyParametersCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        WithoutOpenDialogService dialogService = new();
        IReadOnlyList<string> familyPaths = dialogService.PickRevitFiles();
        if (familyPaths.Count == 0)
        {
            return Result.Cancelled;
        }

        string? sharedParameterFilePath = dialogService.PickSharedParameterFile();
        if (string.IsNullOrWhiteSpace(sharedParameterFilePath))
        {
            return Result.Cancelled;
        }

        BatchAddSharedFamilyParametersService service = new();
        IReadOnlyList<Models.SharedParameterDefinitionItem> definitions = service.LoadDefinitions(commandData.Application.Application, sharedParameterFilePath);
        if (definitions.Count == 0)
        {
            TaskDialog.Show("Add Shared", "Nie znaleziono definicji w wybranym pliku Shared Parameters.");
            return Result.Cancelled;
        }

        BatchAddSharedFamilyParametersWindow window = new(definitions, service.GetGroupOptions(), sharedParameterFilePath);
        if (window.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        var result = service.AddParameters(commandData.Application.Application, familyPaths, sharedParameterFilePath, window.Options);
        TaskDialog.Show(
            "WithoutOpen - Add Shared",
            $"Pliki: {result.Entries.Count}\n" +
            $"Sukces: {result.SuccessCount}\n" +
            $"Pominiete: {result.SkippedCount}\n" +
            $"Bledy: {result.FailedCount}\n\n" +
            $"{string.Join("\n", result.Entries.Take(12).Select(entry => $"{Path.GetFileName(entry.FilePath)} | {entry.Status} | {entry.Message}"))}");

        return Result.Succeeded;
    }
}
