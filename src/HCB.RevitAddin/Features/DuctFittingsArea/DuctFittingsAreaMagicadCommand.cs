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
        DuctFittingsAreaLinearCommand.ShowReport("Duct Fittings Area MAGICAD", "duct-fittings-area-magicad.csv", result);
        return Result.Succeeded;
    }
}
