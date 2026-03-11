using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.ViewsDuplicate.Models;
using HCB.RevitAddin.Features.ViewsDuplicate.UI;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.ViewsDuplicate;

[Transaction(TransactionMode.Manual)]
public sealed class ViewsDuplicateCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        Document document = uiDocument.Document;
        ViewsDuplicateService service = new();

        List<View> views = uiDocument.Selection.GetElementIds()
            .Select(id => document.GetElement(id))
            .OfType<View>()
            .Where(view => view is not ViewSheet)
            .ToList();

        if (views.Count == 0)
        {
            List<View> availableViews = service.GetAvailableViews(document).ToList();
            SelectionListWindow picker = new(
                "Duplicate Views",
                "Wybierz widoki",
                availableViews.Select(ToViewSelectionItem),
                [],
                "Wybierz",
                null,
                availableViews.Contains(uiDocument.ActiveView) ? uiDocument.ActiveView : null,
                "Active View",
                "ViewType",
                "Type");

            if (picker.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            views = picker.SelectedValues.Cast<View>().ToList();
        }

        ViewsDuplicateWindow optionsWindow = new();
        if (optionsWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        ViewsDuplicateResult result = service.Duplicate(document, views, optionsWindow.Options);
        TaskDialog.Show(
            "Duplicate Views",
            $"Widoki zrodlowe: {result.SourceCount}\nUtworzone kopie: {result.CreatedCount}\n\n{string.Join("\n", result.Messages.Take(12))}");

        return Result.Succeeded;
    }

    private static SelectionListItem ToViewSelectionItem(View view)
    {
        return new SelectionListItem(
            view,
            view.Name,
            view.ViewType.ToString(),
            view.IsTemplate ? "Template" : "View");
    }
}
