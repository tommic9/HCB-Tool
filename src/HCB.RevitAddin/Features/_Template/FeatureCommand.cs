using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace HCB.RevitAddin.Features._Template;

[Transaction(TransactionMode.Manual)]
public sealed class FeatureCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        Document document = uiDocument.Document;

        var service = new FeatureService(document);

        // Replace with tool-specific workflow and optional WPF dialog.
        TaskDialog.Show("Template", service.GetPlaceholderMessage());
        return Result.Succeeded;
    }
}
