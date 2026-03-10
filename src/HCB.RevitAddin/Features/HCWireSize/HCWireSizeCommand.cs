using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace HCB.RevitAddin.Features.HCWireSize;

[Transaction(TransactionMode.Manual)]
public sealed class HCWireSizeCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        HCWireSizeService service = new();
        var result = service.Apply(commandData.Application.ActiveUIDocument.Document);

        TaskDialog.Show(
            "HC Wire Size",
            $"Zaktualizowane obwody: {result.UpdatedCount}\n\n{string.Join("\n", result.Messages.Take(12))}");

        return Result.Succeeded;
    }
}
