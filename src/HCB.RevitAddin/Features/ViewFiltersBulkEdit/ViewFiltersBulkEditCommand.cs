using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.ViewFiltersBulkEdit.Models;
using HCB.RevitAddin.Features.ViewFiltersBulkEdit.UI;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.ViewFiltersBulkEdit;

[Transaction(TransactionMode.Manual)]
public sealed class ViewFiltersBulkEditCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document document = commandData.Application.ActiveUIDocument.Document;
        ViewFiltersBulkEditService service = new();

        var viewsWithFilters = service.GetViewsWithFilters(document);
        if (viewsWithFilters.Count == 0)
        {
            TaskDialog.Show("View Filters Bulk Edit", "Brak widokow lub szablonow z filtrami.");
            return Result.Succeeded;
        }

        SelectionListWindow viewsWindow = new(
            "View Filters Bulk Edit",
            "Wybierz widoki",
            viewsWithFilters.Select(view => new SelectionListItem(view, view.Name)));

        if (viewsWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        var selectedViews = viewsWindow.SelectedValues.Cast<View>().ToList();
        var commonFilters = service.GetCommonFilters(document, selectedViews);
        if (commonFilters.Count == 0)
        {
            TaskDialog.Show("View Filters Bulk Edit", "Wybrane widoki nie maja wspolnych filtrow.");
            return Result.Succeeded;
        }

        SelectionListWindow filtersWindow = new(
            "View Filters Bulk Edit",
            "Wybierz filtry",
            commonFilters.Select(filter => new SelectionListItem(filter, filter.Name)));

        if (filtersWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        ViewFiltersBulkEditWindow optionsWindow = new();
        if (optionsWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        ViewFiltersBulkEditResult result = service.Apply(
            document,
            selectedViews,
            filtersWindow.SelectedValues.Cast<ParameterFilterElement>().ToList(),
            optionsWindow.Options);

        TaskDialog.Show(
            "View Filters Bulk Edit",
            $"Zaktualizowane widoki: {result.UpdatedViewsCount}\nOperacje na filtrach: {result.UpdatedFiltersCount}\n\n{string.Join("\n", result.Messages.Take(12))}");

        return Result.Succeeded;
    }
}
