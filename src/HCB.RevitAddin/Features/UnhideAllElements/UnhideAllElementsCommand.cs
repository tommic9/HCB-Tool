using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.UnhideAllElements.Models;

namespace HCB.RevitAddin.Features.UnhideAllElements;

[Transaction(TransactionMode.Manual)]
public sealed class UnhideAllElementsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document document = commandData.Application.ActiveUIDocument.Document;
        View activeView = document.ActiveView;

        UnhideAllElementsService service = new();
        UnhideAllElementsResult result = service.Execute(document, activeView);

        TaskDialog.Show(
            "Unhide All Elements",
            $"Widok: {activeView.Name}\nKandydaci: {result.CandidateCount}\nOdsloniete: {result.UnhiddenCount}");

        return Result.Succeeded;
    }
}
