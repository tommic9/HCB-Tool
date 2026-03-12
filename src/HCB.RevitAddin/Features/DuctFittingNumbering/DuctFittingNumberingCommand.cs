using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.DuctFittingNumbering.Models;
using HCB.RevitAddin.Features.DuctFittingNumbering.UI;

namespace HCB.RevitAddin.Features.DuctFittingNumbering;

[Transaction(TransactionMode.Manual)]
public sealed class DuctFittingNumberingCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        Document document = uiDocument.Document;
        DuctFittingNumberingService service = new();

        var targets = service.CollectTargets(document, uiDocument.Selection.GetElementIds());
        if (targets.Count == 0)
        {
            TaskDialog.Show("Duct and Fitting Numbering", "Brak kanalow lub ksztaltek do numeracji.");
            return Result.Succeeded;
        }

        var availableTargetParameters = service.GetWritableStringTargetParameters(targets);
        var availableLengthParameters = service.GetAvailableLengthParameters(document);
        if (availableTargetParameters.Count == 0)
        {
            TaskDialog.Show("Duct and Fitting Numbering", "Brak zapisywalnych parametrow tekstowych dla wybranych elementow.");
            return Result.Succeeded;
        }

        DuctFittingNumberingWindow window = new(availableTargetParameters, availableLengthParameters);
        if (window.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        DuctFittingNumberingResult result = service.Apply(document, targets, window.SelectedTargetParameter, window.SelectedLengthParameter);
        TaskDialog.Show(
            "Duct and Fitting Numbering",
            $"Ponumerowano: {result.TotalCount}\nKanaly: {result.DuctCount}\nKsztaltki: {result.FittingCount}\nWspoldzielony numer: {result.SharedNumberCount}\nSystemy: {result.SystemsCount}\nParametr docelowy: {result.TargetParameterName}\nParametr dlugosci: {result.LengthParameterName}");

        return Result.Succeeded;
    }
}
