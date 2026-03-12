using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.ViewFiltersLegend.Models;
using HCB.RevitAddin.Features.ViewFiltersLegend.UI;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.ViewFiltersLegend;

[Transaction(TransactionMode.Manual)]
public sealed class ViewFiltersLegendCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        Document document = uiDocument.Document;
        ViewFiltersLegendService service = new();

        var sourceViews = service.GetViewsWithFilters(document);
        if (sourceViews.Count == 0)
        {
            TaskDialog.Show("View Filters Legend", "Brak widokow lub szablonow z przypisanymi filtrami.");
            return Result.Succeeded;
        }

        if (!service.HasLegendBaseView(document))
        {
            TaskDialog.Show("View Filters Legend", "W projekcie musi istniec co najmniej jeden widok legendy.");
            return Result.Succeeded;
        }

        SelectionListWindow viewsWindow = new(
            "View Filters Legend",
            "Widoki i szablony z filtrami",
            sourceViews.Select(ToViewSelectionItem),
            [],
            "Dalej",
            $"Dostepne z filtrami: {sourceViews.Count}",
            sourceViews.Contains(uiDocument.ActiveView) ? uiDocument.ActiveView : null,
            "Active View",
            "ViewType",
            "Type");

        if (viewsWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        var textTypes = service.GetTextNoteTypes(document);
        if (textTypes.Count == 0)
        {
            TaskDialog.Show("View Filters Legend", "Nie znaleziono TextNoteType.");
            return Result.Succeeded;
        }

        ViewFiltersLegendWindow optionsWindow = new(textTypes);
        if (optionsWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        ViewFiltersLegendResult result = service.Generate(
            document,
            viewsWindow.SelectedValues.Cast<View>(),
            optionsWindow.Options);

        if (result.LastLegendViewId.HasValue)
        {
            View? lastLegend = document.GetElement(new ElementId(result.LastLegendViewId.Value)) as View;
            if (lastLegend != null)
            {
                uiDocument.ActiveView = lastLegend;
            }
        }

        List<ReportPreviewColumn> columns =
        [
            new() { Key = "SourceView", Header = "Widok zrodlowy" },
            new() { Key = "LegendView", Header = "Utworzona legenda" },
            new() { Key = "FiltersCount", Header = "Filtry" },
            new() { Key = "LegendId", Header = "ElementId" }
        ];

        IReadOnlyList<IReadOnlyDictionary<string, string>> rows = result.Items
            .Select(item => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>
            {
                ["SourceView"] = item.SourceViewName,
                ["LegendView"] = item.LegendViewName,
                ["FiltersCount"] = item.FiltersCount.ToString(),
                ["LegendId"] = item.LegendViewId.ToString()
            })
            .ToList();

        string summary = $"Utworzone legendy: {result.LegendsCreated}";
        if (result.Messages.Count > 0)
        {
            summary += $"\n{string.Join("\n", result.Messages.Take(6))}";
        }

        ReportPreviewWindow reportWindow = new(
            "View Filters Legend",
            summary,
            columns,
            rows,
            "view-filters-legend.csv",
            null,
            "Open View",
            row =>
            {
                if (!row.TryGetValue("LegendId", out string? legendIdText) ||
                    !long.TryParse(legendIdText, out long legendId))
                {
                    return;
                }

                if (document.GetElement(new ElementId(legendId)) is View legendView)
                {
                    uiDocument.ActiveView = legendView;
                }
            });

        reportWindow.ShowDialog();

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
