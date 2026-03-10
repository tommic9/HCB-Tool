using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HCB.RevitAddin.Features.NumberingSystemElements.Models;

namespace HCB.RevitAddin.Features.NumberingSystemElements;

[Transaction(TransactionMode.Manual)]
public sealed class NumberingSystemElementsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        Reference reference;
        try
        {
            reference = uiDocument.Selection.PickObject(
                ObjectType.Element,
                new MechanicalEquipmentSelectionFilter(),
                "Wskaz Mechanical Equipment jako poczatek numeracji");
        }
        catch
        {
            return Result.Cancelled;
        }

        Element startElement = uiDocument.Document.GetElement(reference);
        NumberingSystemElementsService service = new();
        NumberingSystemElementsResult result = service.Apply(uiDocument.Document, startElement.Id);

        TaskDialog.Show(
            "Numbering System Elements",
            $"Zaktualizowane: {result.UpdatedCount}\nGrupy: {result.GroupCount}\n\n{string.Join("\n", result.Messages)}");

        return Result.Succeeded;
    }

    private sealed class MechanicalEquipmentSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            return element.Category?.Id.Value == (int)BuiltInCategory.OST_MechanicalEquipment;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
