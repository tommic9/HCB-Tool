using System;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.Levels.Models;
using HCB.RevitAddin.Features.Levels.UI;

namespace HCB.RevitAddin.Features.Levels;

[Transaction(TransactionMode.Manual)]
public sealed class LevelsCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        UIDocument? uiDocument = commandData.Application.ActiveUIDocument;
        if (uiDocument?.Document == null)
        {
            TaskDialog.Show("Levels", "To narzedzie wymaga otwartego projektu.");
            return Result.Succeeded;
        }

        try
        {
            LevelsOptionsWindow window = new();
            if (window.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            LevelsService service = new();
            LevelsRenameResult result = service.RenameLevels(uiDocument.Document, window.Options);
            TaskDialog.Show("Levels", BuildSummary(result));
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            TaskDialog.Show("Levels", $"Nie udalo sie zaktualizowac poziomow.\n\n{ex.Message}");
            return Result.Failed;
        }
    }

    private static string BuildSummary(LevelsRenameResult result)
    {
        string summary =
            $"Poziomy znalezione: {result.TotalLevelsCount}\n" +
            $"Zmienione: {result.RenamedLevelsCount}\n" +
            $"Bez zmian lub pominięte: {result.UnchangedLevelsCount}";

        if (result.Messages.Count == 0)
        {
            return summary;
        }

        string details = string.Join("\n", result.Messages.Take(12));
        if (result.Messages.Count > 12)
        {
            details += "\n...";
        }

        return $"{summary}\n\nSzczegóły:\n{details}";
    }
}
