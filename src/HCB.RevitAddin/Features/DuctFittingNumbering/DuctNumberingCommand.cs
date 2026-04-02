using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.DuctFittingNumbering.Models;
using HCB.RevitAddin.Features.DuctFittingNumbering.UI;

namespace HCB.RevitAddin.Features.DuctFittingNumbering;

[Transaction(TransactionMode.Manual)]
public sealed class DuctNumberingCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        Document document = uiDocument.Document;
        bool hasSelection = uiDocument.Selection.GetElementIds().Count > 0;
        string scopeLabel = hasSelection ? "Zaznaczenie" : "Caly model";
        DuctFittingNumberingService service = new();

        var targets = service.CollectTargets(document, uiDocument.Selection.GetElementIds(), DuctFittingNumberingService.NumberingScope.Ducts);
        if (targets.Count == 0)
        {
            TaskDialog.Show("Duct Numbering", "Brak kanalow do numeracji.");
            return Result.Succeeded;
        }

        var availableTargetParameters = service.GetWritableStringTargetParameters(targets);
        var availableLengthParameters = service.GetAvailableLengthParameters(document);
        if (availableTargetParameters.Count == 0)
        {
            TaskDialog.Show("Duct Numbering", "Brak zapisywalnych parametrow tekstowych dla wybranych kanalow.");
            return Result.Succeeded;
        }

        DuctFittingNumberingWindow window = new(
            availableTargetParameters,
            availableLengthParameters,
            "Duct Numbering",
            "Duct Numbering",
            true);

        if (window.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        DuctFittingNumberingResult result = service.Apply(
            document,
            targets,
            window.SelectedTargetParameter,
            window.SelectedLengthParameter,
            window.IncludeSystemParameter,
            window.UseLastProjectNumber,
            DuctFittingNumberingService.NumberingScope.Ducts);

        string messages = result.Messages.Count > 0
            ? "\nUwagi:\n- " + string.Join("\n- ", result.Messages)
            : string.Empty;

        TaskDialog.Show(
            "Duct Numbering",
            $"Tryb: {scopeLabel}\nPonumerowano kanalow: {result.DuctCount}\nWspoldzielony numer: {result.SharedNumberCount}\nSystemy: {result.SystemsCount}\nParametr docelowy: {result.TargetParameterName}\nParametr dlugosci: {result.LengthParameterName}\nDodaj HC_System: {(result.IncludeSystemParameter ? "Tak" : "Nie")}\nStart od ostatniego numeru: {(window.UseLastProjectNumber ? "Tak" : "Nie")}{messages}");

        return Result.Succeeded;
    }
}
