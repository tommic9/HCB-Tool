using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.DuctFittingsArea.Models;
using HCB.RevitAddin.Features.DuctFittingsArea.UI;

namespace HCB.RevitAddin.Features.DuctFittingsArea;

[Transaction(TransactionMode.Manual)]
public sealed class DuctFittingsAreaMagicadCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        DuctFittingsAreaOptionsWindow optionsWindow = new();
        if (optionsWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        DuctFittingsAreaService service = new();
        DuctFittingsAreaResult result = service.CalculateMagicad(commandData.Application.ActiveUIDocument.Document, optionsWindow.Options);
        TaskDialog.Show("Duct Fittings Area MAGICAD", BuildSummary(result));
        return Result.Succeeded;
    }

    private static string BuildSummary(DuctFittingsAreaResult result)
    {
        string summary = $"Przetworzone: {result.ProcessedCount}\nZaktualizowane: {result.UpdatedCount}\nPominięte: {result.SkippedCount}";
        if (result.Messages.Count == 0)
        {
            return summary;
        }

        return $"{summary}\n\nSzczegóły:\n{string.Join("\n", result.Messages.Take(12))}";
    }
}
