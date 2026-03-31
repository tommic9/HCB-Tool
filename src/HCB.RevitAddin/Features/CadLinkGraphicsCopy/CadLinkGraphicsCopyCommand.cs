using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.CadLinkGraphicsCopy;

[Transaction(TransactionMode.Manual)]
public sealed class CadLinkGraphicsCopyCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        Document document = uiDocument.Document;
        View activeView = document.ActiveView;
        CadLinkGraphicsCopyService service = new();

        IReadOnlyList<View> availableViews = service.GetSupportedViews(document);
        if (availableViews.Count == 0)
        {
            TaskDialog.Show("CAD Link Graphics", "Brak obslugiwanych widokow lub template'ow w projekcie.");
            return Result.Succeeded;
        }

        View? sourceView = SelectSingleView(
            service,
            availableViews,
            activeView,
            "CAD Link Graphics",
            "Widok zrodlowy",
            "Wybierz widok lub template, z ktorego chcesz pobrac ustawienia warstw CAD.");

        if (sourceView == null)
        {
            return Result.Cancelled;
        }

        IReadOnlyList<ImportInstance> sourceCandidates = service.GetSelectableCadInstances(document, sourceView);
        if (sourceCandidates.Count == 0)
        {
            TaskDialog.Show("CAD Link Graphics", $"Brak linkow lub importow CAD dostepnych dla {service.GetViewDisplayName(sourceView)}.");
            return Result.Succeeded;
        }

        ImportInstance? sourceInstance = SelectSingleInstance(
            service,
            sourceCandidates,
            "CAD Link Graphics",
            "Link zrodlowy",
            "Wybierz link CAD, z ktorego chcesz skopiowac ustawienia warstw.");

        if (sourceInstance == null)
        {
            return Result.Cancelled;
        }

        View? targetView = SelectSingleView(
            service,
            availableViews,
            activeView,
            "CAD Link Graphics",
            "Widok docelowy",
            "Wybierz widok lub template, do ktorego chcesz skopiowac ustawienia warstw CAD.");

        if (targetView == null)
        {
            return Result.Cancelled;
        }

        IReadOnlyList<ImportInstance> targetCandidates = service.GetSelectableCadInstances(document, targetView);
        if (targetCandidates.Count == 0)
        {
            TaskDialog.Show("CAD Link Graphics", $"Brak linkow lub importow CAD dostepnych dla {service.GetViewDisplayName(targetView)}.");
            return Result.Succeeded;
        }

        ImportInstance? targetInstance = SelectSingleInstance(
            service,
            targetCandidates,
            "CAD Link Graphics",
            "Link docelowy",
            "Wybierz link CAD, do ktorego chcesz skopiowac ustawienia warstw.");

        if (targetInstance == null)
        {
            return Result.Cancelled;
        }

        try
        {
            var result = service.CopyViewOverrides(document, sourceView, targetView, sourceInstance, targetInstance);

            string summary =
                $"Widok zrodlowy: {service.GetViewDisplayName(sourceView)}\n" +
                $"Widok docelowy: {service.GetViewDisplayName(targetView)}\n" +
                $"Link zrodlowy: {service.GetDisplayName(sourceInstance)}\n" +
                $"Link docelowy: {service.GetDisplayName(targetInstance)}\n\n" +
                $"Warstwy zrodla: {result.SourceLayerCount}\n" +
                $"Dopasowane warstwy: {result.MatchedLayerCount}\n" +
                $"Brakujace warstwy: {result.MissingLayerCount}\n" +
                $"Zaktualizowane kategorie: {result.UpdatedCategoryCount}";

            if (result.Messages.Count > 0)
            {
                summary += $"\n\nSzczegoly:\n{string.Join("\n", result.Messages.Take(12))}";
            }

            TaskDialog.Show("CAD Link Graphics", summary);
            return Result.Succeeded;
        }
        catch (Exception exception)
        {
            TaskDialog.Show("CAD Link Graphics", $"Nie udalo sie skopiowac ustawien CAD.\n\n{exception.Message}");
            return Result.Failed;
        }
    }

    private static View? SelectSingleView(
        CadLinkGraphicsCopyService service,
        IReadOnlyList<View> views,
        View activeView,
        string title,
        string sectionTitle,
        string statusText)
    {
        while (true)
        {
            SelectionListWindow window = new(
                title,
                sectionTitle,
                views.Select(view => new SelectionListItem(
                    view.Id,
                    service.GetViewDisplayName(view),
                    view.IsTemplate ? "Template" : "Widok",
                    view.IsTemplate ? "Template" : view.ViewType.ToString())),
                new[] { (object)activeView.Id },
                "Wybierz",
                statusText,
                activeView.Id,
                "Aktywny widok",
                "Typ",
                "Rodzaj");

            if (window.ShowDialog() != true)
            {
                return null;
            }

            IReadOnlyList<ElementId> selectedIds = window.SelectedValues.Cast<ElementId>().ToList();
            if (selectedIds.Count == 1)
            {
                ElementId selectedId = selectedIds[0];
                return views.FirstOrDefault(view => view.Id == selectedId);
            }

            TaskDialog.Show(title, "Wybierz dokladnie jeden widok lub template.");
        }
    }

    private static ImportInstance? SelectSingleInstance(
        CadLinkGraphicsCopyService service,
        IReadOnlyList<ImportInstance> instances,
        string title,
        string sectionTitle,
        string statusText)
    {
        while (true)
        {
            SelectionListWindow window = new(
                title,
                sectionTitle,
                instances.Select(instance => new SelectionListItem(
                    instance.Id,
                    service.GetDisplayName(instance),
                    instance.IsLinked ? "Link" : "Import",
                    instance.ViewSpecific ? "Widok-specific" : "Model-wide")),
                Array.Empty<object>(),
                "Wybierz",
                statusText,
                null,
                "Aktywny element",
                "Typ",
                "Zakres");

            if (window.ShowDialog() != true)
            {
                return null;
            }

            IReadOnlyList<ElementId> selectedIds = window.SelectedValues.Cast<ElementId>().ToList();
            if (selectedIds.Count == 1)
            {
                ElementId selectedId = selectedIds[0];
                return instances.FirstOrDefault(instance => instance.Id == selectedId);
            }

            TaskDialog.Show(title, "Wybierz dokladnie jeden link CAD.");
        }
    }
}
