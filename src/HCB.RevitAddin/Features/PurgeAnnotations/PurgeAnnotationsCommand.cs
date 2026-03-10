using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.PurgeAnnotations.Models;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.PurgeAnnotations;

[Transaction(TransactionMode.Manual)]
public sealed class PurgeAnnotationsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document document = commandData.Application.ActiveUIDocument.Document;
        PurgeAnnotationsService service = new();
        var unusedTypes = service.GetUnusedAnnotationTypes(document);

        if (unusedTypes.Count == 0)
        {
            TaskDialog.Show("Purge Annotations", "Brak nieuzywanych stylow adnotacji w projekcie.");
            return Result.Succeeded;
        }

        SelectionListWindow window = new(
            "Purge Annotations",
            "Nieuzywane style adnotacji",
            unusedTypes.Select(type => new SelectionListItem(type, service.GetFilterableLabel(type), type.Category?.Name)),
            [],
            "Usun wybrane",
            $"Dostepne do usuniecia: {unusedTypes.Count}. Wyszukiwanie dziala po nazwie, kategorii i klasie.");

        if (window.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        PurgeAnnotationsResult result = service.DeleteTypes(document, window.SelectedValues.Cast<ElementType>());
        TaskDialog.Show(
            "Purge Annotations",
            $"Usuniete: {result.DeletedCount}\nBledy: {result.FailedCount}\n\n{string.Join("\n", result.Messages.Take(12))}");

        return Result.Succeeded;
    }
}
