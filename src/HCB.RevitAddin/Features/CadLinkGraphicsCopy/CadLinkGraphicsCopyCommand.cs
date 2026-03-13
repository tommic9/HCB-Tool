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

        IReadOnlyList<ImportInstance> availableInstances = service.GetAvailableCadInstances(document);
        if (availableInstances.Count < 2)
        {
            TaskDialog.Show("CAD Link Graphics", "Potrzebne sa co najmniej dwa linki lub importy CAD w projekcie.");
            return Result.Succeeded;
        }

        ImportInstance? sourceInstance = SelectSingleInstance(
            service,
            availableInstances,
            "CAD Link Graphics",
            "Link zrodlowy",
            "Wybierz link CAD, z ktorego chcesz skopiowac nadpisania i widocznosc warstw.");

        if (sourceInstance == null)
        {
            return Result.Cancelled;
        }

        IReadOnlyList<ImportInstance> targetCandidates = availableInstances
            .Where(instance => instance.Id != sourceInstance.Id)
            .ToList();

        ImportInstance? targetInstance = SelectSingleInstance(
            service,
            targetCandidates,
            "CAD Link Graphics",
            "Link docelowy",
            "Wybierz link CAD, do ktorego chcesz skopiowac ustawienia z aktywnego widoku.");

        if (targetInstance == null)
        {
            return Result.Cancelled;
        }

        try
        {
            var result = service.CopyViewOverrides(document, activeView, sourceInstance, targetInstance);

            string summary =
                $"Widok: {activeView.Name}\n" +
                $"Zrodlo: {service.GetDisplayName(sourceInstance)}\n" +
                $"Cel: {service.GetDisplayName(targetInstance)}\n\n" +
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

    private static ImportInstance? SelectSingleInstance(
        CadLinkGraphicsCopyService service,
        IReadOnlyList<ImportInstance> instances,
        string title,
        string sectionTitle,
        string statusText)
    {
        SelectionListWindow window = new(
            title,
            sectionTitle,
            instances.Select(instance => new SelectionListItem(
                instance.Id,
                service.GetDisplayName(instance),
                instance.IsLinked ? "Link" : "Import")),
            Array.Empty<object>(),
            "Wybierz",
            statusText,
            null,
            "Aktywny element",
            "Typ");

        if (window.ShowDialog() != true)
        {
            return null;
        }

        IReadOnlyList<ElementId> selectedIds = window.SelectedValues.Cast<ElementId>().ToList();
        if (selectedIds.Count != 1)
        {
            TaskDialog.Show(title, "Wybierz dokladnie jeden link CAD.");
            return null;
        }

        ElementId selectedId = selectedIds[0];
        return instances.FirstOrDefault(instance => instance.Id == selectedId);
    }
}
