using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.DuctFittingNumbering.Models;
using HCB.RevitAddin.Features.DuctFittingNumbering.UI;

namespace HCB.RevitAddin.Features.DuctFittingNumbering;

[Transaction(TransactionMode.Manual)]
public sealed class DuctFittingsNumberingCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        Document document = uiDocument.Document;
        bool hasSelection = uiDocument.Selection.GetElementIds().Count > 0;
        string scopeLabel = hasSelection ? "Zaznaczenie" : "Caly model";
        DuctFittingNumberingService service = new();

        var targets = service.CollectTargets(document, uiDocument.Selection.GetElementIds(), DuctFittingNumberingService.NumberingScope.Fittings);
        if (targets.Count == 0)
        {
            TaskDialog.Show("Duct Fitting Numbering", "Brak ksztaltek lub akcesoriow do numeracji.");
            return Result.Succeeded;
        }

        var availableTargetParameters = service.GetWritableStringTargetParameters(targets);
        if (availableTargetParameters.Count == 0)
        {
            TaskDialog.Show("Duct Fitting Numbering", "Brak zapisywalnych parametrow tekstowych dla wybranych ksztaltek lub akcesoriow.");
            return Result.Succeeded;
        }

        DuctFittingNumberingWindow window = new(
            availableTargetParameters,
            [],
            "Duct Fitting Numbering",
            "Duct Fitting and Accessory Numbering",
            false);

        if (window.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        DuctFittingNumberingResult result = service.Apply(
            document,
            targets,
            window.SelectedTargetParameter,
            null,
            window.IncludeSystemParameter,
            DuctFittingNumberingService.NumberingScope.Fittings);

        string messages = result.Messages.Count > 0
            ? "\nUwagi:\n- " + string.Join("\n- ", result.Messages)
            : string.Empty;

        TaskDialog.Show(
            "Duct Fitting Numbering",
            $"Tryb: {scopeLabel}\nPonumerowano ksztaltek: {result.FittingCount}\nPonumerowano akcesoriow: {result.AccessoryCount}\nWspoldzielony numer: {result.SharedNumberCount}\nSystemy: {result.SystemsCount}\nParametr docelowy: {result.TargetParameterName}\nDodaj HC_System: {(result.IncludeSystemParameter ? "Tak" : "Nie")}{messages}");

        return Result.Succeeded;
    }
}
