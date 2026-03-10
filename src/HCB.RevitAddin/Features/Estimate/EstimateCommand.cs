using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.Estimate.Models;
using HCB.RevitAddin.Features.Estimate.UI;

namespace HCB.RevitAddin.Features.Estimate;

[Transaction(TransactionMode.Manual)]
public sealed class EstimateCommand : IExternalCommand
{
    private static string _lastCatalogPath = string.Empty;

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document document = commandData.Application.ActiveUIDocument.Document;
        EstimateOptionsWindow window = new(_lastCatalogPath);
        if (window.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        EstimateOptions options = window.Options;
        _lastCatalogPath = options.CatalogPath;

        EstimateService service = new();
        EstimateResult result = service.Apply(document, options);

        string summary = $"Zaktualizowane: {result.UpdatedCount}\nBrak dopasowania: {result.UnmatchedCount}";
        if (result.MissingAngleElementIds.Count > 0)
        {
            summary += $"\nBrak HC_Kat: {result.MissingAngleElementIds.Count}";
        }

        if (result.UnmatchedRows.Count > 0)
        {
            summary += $"\n\n{string.Join("\n", result.UnmatchedRows.Take(8).Select(row => $"{row.ElementId}: {row.Reason}"))}";
        }

        TaskDialog.Show("Estimate", summary);
        return Result.Succeeded;
    }
}
