using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace HCB.RevitAddin.Features.SpaceToElement;

[Transaction(TransactionMode.Manual)]
public sealed class SpaceToElementCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        SpaceToElementService service = new();
        var result = service.Apply(uiDocument.Document, uiDocument.ActiveView);

        TaskDialog.Show(
            "Space To Element",
            $"Zaktualizowane: {result.UpdatedCount}\nPominiete: {result.SkippedCount}\nBledy: {result.ErrorCount}\n\n{string.Join("\n", result.Messages.Take(10))}");

        return Result.Succeeded;
    }
}
