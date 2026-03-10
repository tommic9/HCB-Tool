using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.SpaceParamLinked.Models;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.SpaceParamLinked;

[Transaction(TransactionMode.Manual)]
public sealed class SpaceParamLinkedCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document document = commandData.Application.ActiveUIDocument.Document;
        SpaceParamLinkedService service = new();
        IReadOnlyList<RevitLinkInstance> availableLinks = service.GetAvailableLinks(document);

        if (availableLinks.Count == 0)
        {
            TaskDialog.Show("SpaceParamLinked", "Brak zaladowanych linkow Revit do przetworzenia.");
            return Result.Succeeded;
        }

        SelectionListWindow window = new(
            "SpaceParamLinked",
            "Linki Revit",
            availableLinks.Select(link => new SelectionListItem(link.Id, BuildDisplayName(link))),
            availableLinks.Select(link => (object)link.Id),
            "Aktualizuj parametry",
            "Wybierz linki, z ktorych chcesz pobrac pokoje dla elementow w modelu.");

        if (window.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        IReadOnlyList<ElementId> selectedLinkIds = window.SelectedValues.Cast<ElementId>().ToList();
        SpaceParamLinkedResult result = service.UpdateFromLinkedRooms(document, selectedLinkIds);

        string summary =
            $"Przetworzone elementy: {result.ProcessedCount}\n" +
            $"Zaktualizowane: {result.UpdatedCount}\n" +
            $"Bez dopasowania: {result.NotFoundCount}";

        if (result.Messages.Count > 0)
        {
            summary += $"\n\nSzczegoly:\n{string.Join("\n", result.Messages.Take(12))}";
        }

        TaskDialog.Show("SpaceParamLinked", summary);
        return Result.Succeeded;
    }

    private static string BuildDisplayName(RevitLinkInstance link)
    {
        string documentName = link.GetLinkDocument()?.Title ?? "Brak dokumentu";
        return $"{link.Name} [{documentName}]";
    }
}
