using System;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.AccessoryTerminalNumbering.Models;
using HCB.RevitAddin.Features.AccessoryTerminalNumbering.UI;

namespace HCB.RevitAddin.Features.AccessoryTerminalNumbering;

[Transaction(TransactionMode.Manual)]
public sealed class AccessoryTerminalNumberingCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument? uiDocument = commandData.Application.ActiveUIDocument;
        if (uiDocument?.Document == null)
        {
            TaskDialog.Show("MEP Item Numbering", "To narzedzie wymaga otwartego projektu.");
            return Result.Succeeded;
        }

        try
        {
            Document document = uiDocument.Document;
            AccessoryTerminalNumberingService service = new();

            var targets = service.CollectTargets(document, uiDocument.Selection.GetElementIds());
            if (targets.Count == 0)
            {
                TaskDialog.Show("MEP Item Numbering", "Brak akcesoriow kanalowych, akcesoriow rurowych lub terminali do numeracji.");
                return Result.Succeeded;
            }

            var availableTargetParameters = service.GetWritableStringTargetParameters(targets);
            if (availableTargetParameters.Count == 0)
            {
                TaskDialog.Show("MEP Item Numbering", "Brak zapisywalnych parametrow tekstowych dla wybranych elementow.");
                return Result.Succeeded;
            }

            var availableAccessoryTypeParameters = service.GetAccessoryTypeParameterNames(document, targets);
            bool hasDuctAccessories = targets.Any(element => element.Category?.Id.Value == (long)BuiltInCategory.OST_DuctAccessory);
            bool hasPipeAccessories = targets.Any(element => element.Category?.Id.Value == (long)BuiltInCategory.OST_PipeAccessory);
            bool hasAirTerminals = targets.Any(element => element.Category?.Id.Value == (long)BuiltInCategory.OST_DuctTerminal);

            AccessoryTerminalNumberingWindow window = new(
                availableTargetParameters,
                availableAccessoryTypeParameters,
                hasDuctAccessories,
                hasPipeAccessories,
                hasAirTerminals);

            if (window.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            AccessoryTerminalNumberingResult result = service.Apply(document, targets, window.Options);
            TaskDialog.Show(
                "MEP Item Numbering",
                $"Ponumerowano: {result.TotalCount}\nAkcesoria kanalowe: {result.DuctAccessoryCount}\nAkcesoria rurowe: {result.PipeAccessoryCount}\nTerminale: {result.TerminalCount}\nWspoldzielony numer: {result.SharedNumberCount}\nSystemy: {result.SystemsCount}\nParametr docelowy: {result.TargetParameterName}\nParametr typu akcesoriow: {(string.IsNullOrWhiteSpace(result.AccessoryTypeParameterName) ? "(brak)" : result.AccessoryTypeParameterName)}");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            TaskDialog.Show("MEP Item Numbering", $"Nie udalo sie wykonac numeracji.\n\n{ex.Message}");
            return Result.Failed;
        }
    }
}
