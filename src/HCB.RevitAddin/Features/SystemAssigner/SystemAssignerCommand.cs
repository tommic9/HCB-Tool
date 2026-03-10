using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace HCB.RevitAddin.Features.SystemAssigner;

[Transaction(TransactionMode.Manual)]
public sealed class SystemAssignerCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        Document document = uiDocument.Document;

        IList<Reference> references;
        try
        {
            references = uiDocument.Selection.PickObjects(ObjectType.Element, new MechanicalEquipmentSelectionFilter(), "Wybierz urzadzenia MEP");
        }
        catch
        {
            return Result.Cancelled;
        }

        List<Element> equipment = references
            .Select(reference => document.GetElement(reference))
            .Where(element => element?.Category?.Id.Value == (long)BuiltInCategory.OST_MechanicalEquipment)
            .Cast<Element>()
            .ToList();

        if (equipment.Count == 0)
        {
            TaskDialog.Show("System Assigner", "Nie wybrano zadnych urzadzen Mechanical Equipment.");
            return Result.Succeeded;
        }

        SystemAssignerService service = new();
        var result = service.Apply(document, equipment);
        TaskDialog.Show(
            "System Assigner",
            $"Urzadzenia: {result.ProcessedEquipmentCount}\nSystemy: {result.ProcessedSystemCount}\nZaktualizowane elementy: {result.ChangedCount}\n\n{string.Join("\n", result.Messages.Take(12))}");
        return Result.Succeeded;
    }

    private sealed class MechanicalEquipmentSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element element) => element.Category?.Id.Value == (long)BuiltInCategory.OST_MechanicalEquipment;

        public bool AllowReference(Reference reference, XYZ position) => true;
    }
}
