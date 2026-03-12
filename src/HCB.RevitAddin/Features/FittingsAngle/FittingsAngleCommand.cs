using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.FittingsAngle;

[Transaction(TransactionMode.Manual)]
public sealed class FittingsAngleCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        Document document = uiDocument.Document;
        FittingsAngleService service = new();

        SelectionListWindow categoriesWindow = new(
            "Fittings Angle",
            "Wybierz kategorie",
            service.GetCategoryNames().Select(name => new SelectionListItem(name, name)),
            [],
            "Dalej",
            "Jesli masz zaznaczenie, narzedzie przetworzy tylko wybrane elementy z tych kategorii.");

        if (categoriesWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        var selectedCategories = categoriesWindow.SelectedValues.Cast<string>().ToList();

        var availableParameterNames = service.GetAvailableAngleParameterNames(
            document,
            uiDocument.Selection.GetElementIds(),
            selectedCategories);

        SelectionListWindow sourceParameterWindow = new(
            "Fittings Angle",
            "Wybierz dodatkowy parametr kata",
            new[] { new SelectionListItem(FittingsAngleService.NoExtraParameterOption, "Brak dodatkowego parametru") }
                .Concat(availableParameterNames.Select(name => new SelectionListItem(name, name))),
            new object[] { FittingsAngleService.NoExtraParameterOption },
            "Dalej",
            "Wybierz opcjonalny dodatkowy parametr zrodlowy. Jesli zaznaczysz kilka pozycji, narzedzie uzyje pierwszej.");

        if (sourceParameterWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        string? extraParameterName = sourceParameterWindow.SelectedValues
            .Cast<string>()
            .FirstOrDefault();

        var availableTargetParameters = service.GetAvailableTargetParameterNames(
            document,
            uiDocument.Selection.GetElementIds(),
            selectedCategories);

        if (availableTargetParameters.Count == 0)
        {
            TaskDialog.Show("Fittings Angle", "Nie znaleziono zadnego zapisywalnego parametru typu Angle dla wybranych elementow.");
            return Result.Cancelled;
        }

        object[] defaultTargetSelection = availableTargetParameters
            .Any(name => string.Equals(name, FittingsAngleService.DefaultTargetParameterName, System.StringComparison.OrdinalIgnoreCase))
            ? new object[] { availableTargetParameters.First(name => string.Equals(name, FittingsAngleService.DefaultTargetParameterName, System.StringComparison.OrdinalIgnoreCase)) }
            : new object[] { availableTargetParameters[0] };

        SelectionListWindow targetParameterWindow = new(
            "Fittings Angle",
            "Wybierz parametr docelowy",
            availableTargetParameters.Select(name => new SelectionListItem(name, name)),
            defaultTargetSelection,
            "Uruchom",
            "Wybierz parametr docelowy typu Angle. Lista pokazuje tylko parametry projektu lub wspoldzielone przypiete do wybranych kategorii. Narzedzie wpisze do niego zaokraglony kat pobrany z elementu. Jesli zaznaczysz kilka pozycji, narzedzie uzyje pierwszej.");

        if (targetParameterWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        string targetParameterName = targetParameterWindow.SelectedValues
            .Cast<string>()
            .First();

        var result = service.Apply(
            document,
            uiDocument.Selection.GetElementIds(),
            selectedCategories,
            extraParameterName,
            targetParameterName);

        if (result.FailedElementIds.Count > 0)
        {
            uiDocument.Selection.SetElementIds(result.FailedElementIds.Select(id => new ElementId(id)).ToList());
        }

        TaskDialog.Show(
            "Fittings Angle",
            $"Parametr docelowy: {targetParameterName}\n\nKandydaci: {result.CandidateCount}\nZaktualizowane: {result.UpdatedCount}\nBrak zrodla kata: {result.MissingSourceCount}\nBrak lub blad parametru docelowego: {result.MissingTargetCount}");

        return Result.Succeeded;
    }
}
