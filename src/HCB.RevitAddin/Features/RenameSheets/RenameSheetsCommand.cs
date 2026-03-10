using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.RenameSheets.Models;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.RenameSheets;

[Transaction(TransactionMode.Manual)]
public sealed class RenameSheetsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        Document document = uiDocument.Document;

        List<ViewSheet> sheets = uiDocument.Selection.GetElementIds()
            .Select(id => document.GetElement(id))
            .OfType<ViewSheet>()
            .ToList();

        if (sheets.Count == 0)
        {
            SelectionListWindow picker = new(
                "Zmiana nazw arkuszy",
                "Wybierz arkusze",
                new FilteredElementCollector(document)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .Select(sheet => new SelectionListItem(sheet, $"{sheet.SheetNumber} | {sheet.Name}")));

            if (picker.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            sheets = picker.SelectedValues.Cast<ViewSheet>().ToList();
        }

        RenameOptionsWindow window = new("Zmiana nazw arkuszy", RenameOptionsMode.Sheets);
        if (window.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        RenameSheetsService service = new();
        RenameSheetsResult result = service.Rename(document, sheets, window.Options);
        TaskDialog.Show("Rename Sheets", BuildSummary(result));
        return Result.Succeeded;
    }

    private static string BuildSummary(RenameSheetsResult result)
    {
        string summary = $"Przetworzone arkusze: {result.ProcessedCount}\nZmienione arkusze: {result.RenamedCount}";
        if (result.Messages.Count == 0)
        {
            return summary;
        }

        return $"{summary}\n\nSzczegoly:\n{string.Join("\n", result.Messages.Take(12))}";
    }
}
