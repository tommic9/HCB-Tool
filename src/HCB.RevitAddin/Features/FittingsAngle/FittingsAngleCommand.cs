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

        var availableParameterNames = service.GetAvailableAngleParameterNames(
            document,
            uiDocument.Selection.GetElementIds(),
            categoriesWindow.SelectedValues.Cast<string>().ToList());

        SelectionListWindow parameterWindow = new(
            "Fittings Angle",
            "Wybierz dodatkowy parametr kata",
            new[] { new SelectionListItem(FittingsAngleService.NoExtraParameterOption, "Brak dodatkowego parametru") }
                .Concat(availableParameterNames.Select(name => new SelectionListItem(name, name))),
            new object[] { FittingsAngleService.NoExtraParameterOption },
            "Uruchom",
            "Wybierz opcjonalny dodatkowy parametr zrodlowy. Jesli zaznaczysz kilka pozycji, narzedzie uzyje pierwszej.");

        if (parameterWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        string? extraParameterName = parameterWindow.SelectedValues
            .Cast<string>()
            .FirstOrDefault();

        var result = service.Apply(
            document,
            uiDocument.Selection.GetElementIds(),
            categoriesWindow.SelectedValues.Cast<string>().ToList(),
            extraParameterName);

        if (result.FailedElementIds.Count > 0)
        {
            uiDocument.Selection.SetElementIds(result.FailedElementIds.Select(id => new ElementId(id)).ToList());
        }

        TaskDialog.Show(
            "Fittings Angle",
            $"Kandydaci: {result.CandidateCount}\nZaktualizowane: {result.UpdatedCount}\nBrak zrodla kata: {result.MissingSourceCount}\nBrak lub blad HC_Kat: {result.MissingTargetCount}");

        return Result.Succeeded;
    }
}
