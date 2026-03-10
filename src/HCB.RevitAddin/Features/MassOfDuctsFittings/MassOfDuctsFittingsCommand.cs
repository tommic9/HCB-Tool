using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace HCB.RevitAddin.Features.MassOfDuctsFittings;

[Transaction(TransactionMode.Manual)]
public sealed class MassOfDuctsFittingsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        MassOfDuctsFittingsService service = new();
        var result = service.Apply(uiDocument.Document, uiDocument.Selection.GetElementIds());

        TaskDialog.Show(
            "Mass Of Ducts & Fittings",
            $"Zaktualizowane: {result.UpdatedCount}\nPominiete: {result.SkippedCount}\n\n{string.Join("\n", result.Messages.Take(12))}");

        return Result.Succeeded;
    }
}
