using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.LevelFromHVACElements;

[Transaction(TransactionMode.Manual)]
public sealed class LevelFromHVACElementsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument? uiDocument = commandData.Application.ActiveUIDocument;
        if (uiDocument?.Document == null)
        {
            TaskDialog.Show("Level From HVAC Elements", "To narzedzie wymaga otwartego projektu.");
            return Result.Succeeded;
        }

        try
        {
            Document document = uiDocument.Document;
            LevelFromHVACElementsService service = new();

            IList<Reference> references;
            try
            {
                references = uiDocument.Selection.PickObjects(ObjectType.Element, new SupportedHvacSelectionFilter(service), "Wybierz elementy HVAC");
            }
            catch
            {
                return Result.Cancelled;
            }

            List<Element> selectedElements = references
                .Select(reference => document.GetElement(reference))
                .Where(service.IsSupported)
                .ToList();

            if (selectedElements.Count == 0)
            {
                TaskDialog.Show("Level From HVAC Elements", "Nie wybrano zadnych obslugiwanych elementow HVAC.");
                return Result.Succeeded;
            }

            IReadOnlyList<string> parameterNames = service.GetEditableTargetParameterNames(selectedElements);
            if (parameterNames.Count == 0)
            {
                TaskDialog.Show("Level From HVAC Elements", "Nie znaleziono wspolnych bezpiecznych parametrow docelowych dla zaznaczonych elementow.");
                return Result.Succeeded;
            }

            SelectionListWindow parameterWindow = new(
                "Level From HVAC Elements",
                "Wybierz parametr docelowy",
                parameterNames.Select(name => new SelectionListItem(name, name)),
                [],
                "Kopiuj",
                "Narzedzie kopiuje Level lub Reference Level do wybranego parametru instancyjnego.");

            if (parameterWindow.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            string targetParameterName = parameterWindow.SelectedValues.Cast<string>().First();
            var result = service.Apply(document, selectedElements, targetParameterName);

            if (result.FailedElementIds.Count > 0)
            {
                uiDocument.Selection.SetElementIds(result.FailedElementIds.Select(id => new ElementId(id)).ToList());
            }

            TaskDialog.Show(
                "Level From HVAC Elements",
                $"Zaktualizowane: {result.UpdatedCount}\nPominiete lub bledne: {result.FailedCount}");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            TaskDialog.Show("Level From HVAC Elements", $"Nie udalo sie skopiowac poziomu.\n\n{ex.Message}");
            return Result.Failed;
        }
    }

    private sealed class SupportedHvacSelectionFilter(LevelFromHVACElementsService service) : ISelectionFilter
    {
        public bool AllowElement(Element element) => service.IsSupported(element);

        public bool AllowReference(Reference reference, XYZ position) => true;
    }
}
