using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.RenameViews.Models;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.RenameViews;

[Transaction(TransactionMode.Manual)]
public sealed class RenameViewsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        Document document = uiDocument.Document;

        List<View> views = uiDocument.Selection.GetElementIds()
            .Select(id => document.GetElement(id))
            .OfType<View>()
            .Where(view => view is not ViewSheet)
            .ToList();

        if (views.Count == 0)
        {
            SelectionListWindow picker = new(
                "Zmiana nazw widokow",
                "Wybierz widoki",
                new FilteredElementCollector(document)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .Where(view => view is not ViewSheet)
                    .Select(view => new SelectionListItem(view, view.Name)));

            if (picker.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            views = picker.SelectedValues.Cast<View>().ToList();
        }

        RenameOptionsWindow window = new("Zmiana nazw widokow", RenameOptionsMode.Views);
        if (window.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        RenameViewsService service = new();
        RenameViewsResult result = service.Rename(document, views, window.Options);
        TaskDialog.Show("Rename Views", BuildSummary(result));
        return Result.Succeeded;
    }

    private static string BuildSummary(RenameViewsResult result)
    {
        string summary = $"Przetworzone widoki: {result.ProcessedCount}\nZmienione nazwy: {result.RenamedCount}";
        if (result.Messages.Count == 0)
        {
            return summary;
        }

        return $"{summary}\n\nSzczegoly:\n{string.Join("\n", result.Messages.Take(12))}";
    }
}
