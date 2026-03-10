using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using HCB.RevitAddin.Features.SpaceToElement.Models;

namespace HCB.RevitAddin.Features.SpaceToElement;

public sealed class SpaceToElementService
{
    private static readonly BuiltInCategory[] TargetCategories =
    [
        BuiltInCategory.OST_MechanicalEquipment,
        BuiltInCategory.OST_DuctCurves,
        BuiltInCategory.OST_FlexDuctCurves,
        BuiltInCategory.OST_DuctFitting,
        BuiltInCategory.OST_DuctAccessory,
        BuiltInCategory.OST_DuctTerminal,
        BuiltInCategory.OST_PipeCurves,
        BuiltInCategory.OST_FlexPipeCurves,
        BuiltInCategory.OST_PipeFitting,
        BuiltInCategory.OST_PipeAccessory,
        BuiltInCategory.OST_PlumbingEquipment,
        BuiltInCategory.OST_CommunicationDevices,
        BuiltInCategory.OST_PlumbingFixtures,
        BuiltInCategory.OST_ElectricalEquipment,
        BuiltInCategory.OST_ElectricalFixtures,
        BuiltInCategory.OST_LightingFixtures
    ];

    public SpaceToElementResult Apply(Document document, View activeView)
    {
        List<Space> spaces = new FilteredElementCollector(document, activeView.Id)
            .OfCategory(BuiltInCategory.OST_MEPSpaces)
            .WhereElementIsNotElementType()
            .Cast<Space>()
            .ToList();

        SpaceToElementResult result = new();
        if (spaces.Count == 0)
        {
            result.Messages.Add("Brak przestrzeni MEP w aktywnym widoku.");
            return result;
        }

        List<Element> elements = [];
        foreach (BuiltInCategory category in TargetCategories)
        {
            elements.AddRange(new FilteredElementCollector(document)
                .OfCategory(category)
                .WhereElementIsNotElementType()
                .ToElements());
        }

        using Transaction transaction = new(document, "Space To Element");
        transaction.Start();

        foreach (Element element in elements)
        {
            try
            {
                XYZ? point = GetRepresentativePoint(element, activeView);
                if (point == null)
                {
                    result.SkippedCount++;
                    continue;
                }

                Space? space = spaces.FirstOrDefault(candidate => candidate.IsPointInSpace(point));
                if (space == null)
                {
                    result.SkippedCount++;
                    continue;
                }

                Parameter? numberParameter = element.LookupParameter("LIN_ROOM_NUMBER");
                Parameter? nameParameter = element.LookupParameter("LIN_ROOM_NAME");
                if (numberParameter == null || nameParameter == null || numberParameter.IsReadOnly || nameParameter.IsReadOnly)
                {
                    result.SkippedCount++;
                    continue;
                }

                string roomNumber = space.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.AsString() ?? string.Empty;
                string roomName = space.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? string.Empty;

                numberParameter.Set(roomNumber);
                nameParameter.Set(roomName);
                result.UpdatedCount++;
            }
            catch (Exception ex)
            {
                result.ErrorCount++;
                result.Messages.Add($"Element {element.Id.Value}: {ex.Message}");
            }
        }

        transaction.Commit();
        return result;
    }

    private static XYZ? GetRepresentativePoint(Element element, View activeView)
    {
        switch (element.Location)
        {
            case LocationPoint locationPoint:
                return locationPoint.Point;
            case LocationCurve locationCurve:
                return locationCurve.Curve.Evaluate(0.5, true);
        }

        BoundingBoxXYZ? boundingBox = element.get_BoundingBox(activeView) ?? element.get_BoundingBox(null);
        if (boundingBox == null)
        {
            return null;
        }

        return (boundingBox.Min + boundingBox.Max) * 0.5;
    }
}
