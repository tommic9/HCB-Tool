using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.FlowChanger.Models;
using HCB.RevitAddin.Features.FlowChanger.UI;

namespace HCB.RevitAddin.Features.FlowChanger;

[Transaction(TransactionMode.Manual)]
public sealed class FlowChangerCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document document = commandData.Application.ActiveUIDocument.Document;
        FlowChangerService service = new();
        IReadOnlyList<string> parameterNames = service.GetAvailableTargetParameters(document);

        if (parameterNames.Count == 0)
        {
            TaskDialog.Show("FlowChanger", "Nie znaleziono zapisywalnych parametrow typu liczbowego na terminalach w aktywnym widoku.");
            return Result.Cancelled;
        }

        FlowChangerOptionsWindow window = new(parameterNames);
        if (window.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        FlowChangerResult result = service.UpdateActualFlow(document, window.SelectedParameterName);
        TaskDialog.Show("FlowChanger", BuildSummary(result));
        return Result.Succeeded;
    }

    private static string BuildSummary(FlowChangerResult result)
    {
        string summary = $"Elementy w aktywnym widoku: {result.ProcessedCount}\nZaktualizowane: {result.UpdatedCount}";
        if (result.Messages.Count == 0)
        {
            return summary;
        }

        return $"{summary}\n\nSzczegoly:\n{string.Join("\n", result.Messages.Take(12))}";
    }
}
