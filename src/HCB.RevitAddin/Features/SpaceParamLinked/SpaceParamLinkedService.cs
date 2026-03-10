using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using HCB.RevitAddin.Features.SpaceParamLinked.Models;

namespace HCB.RevitAddin.Features.SpaceParamLinked;

public sealed class SpaceParamLinkedService
{
    public IReadOnlyList<RevitLinkInstance> GetAvailableLinks(Document document)
    {
        return new FilteredElementCollector(document)
            .OfClass(typeof(RevitLinkInstance))
            .Cast<RevitLinkInstance>()
            .Where(link => link.GetLinkDocument() != null)
            .OrderBy(link => link.Name)
            .ToList();
    }

    public SpaceParamLinkedResult UpdateFromLinkedRooms(Document document, IEnumerable<ElementId> selectedLinkIds)
    {
        List<FamilyInstance> equipment = new FilteredElementCollector(document)
            .OfCategory(BuiltInCategory.OST_MechanicalEquipment)
            .WhereElementIsNotElementType()
            .OfType<FamilyInstance>()
            .ToList();

        HashSet<long> selectedLinkIdValues = selectedLinkIds.Select(id => id.Value).ToHashSet();
        List<RevitLinkInstance> links = GetAvailableLinks(document)
            .Where(link => selectedLinkIdValues.Contains(link.Id.Value))
            .ToList();

        SpaceParamLinkedResult result = new();
        if (links.Count == 0)
        {
            result.Messages.Add("Nie wybrano zadnego aktywnego linku do przetworzenia.");
            return result;
        }

        using Transaction transaction = new(document, "SpaceParamLinked");
        transaction.Start();

        foreach (FamilyInstance element in equipment)
        {
            result.ProcessedCount++;

            XYZ? point = GetElementPoint(element);
            if (point == null)
            {
                result.NotFoundCount++;
                continue;
            }

            bool matchedRoom = false;
            foreach (RevitLinkInstance link in links)
            {
                Document linkedDocument = link.GetLinkDocument()!;
                Transform inverse = link.GetTotalTransform().Inverse;
                XYZ linkedPoint = inverse.OfPoint(point);
                Room? room = linkedDocument.GetRoomAtPoint(linkedPoint);

                if (room == null)
                {
                    continue;
                }

                matchedRoom = true;

                Parameter? numberParameter = element.LookupParameter("LIN_ROOM_NUMBER");
                Parameter? nameParameter = element.LookupParameter("LIN_ROOM_NAME");

                if (numberParameter == null || nameParameter == null || numberParameter.IsReadOnly || nameParameter.IsReadOnly)
                {
                    result.Messages.Add($"Element {element.Id.Value}: brak parametrow LIN_ROOM_NUMBER lub LIN_ROOM_NAME.");
                    break;
                }

                numberParameter.Set(room.Number ?? string.Empty);
                nameParameter.Set(room.Name ?? string.Empty);
                result.UpdatedCount++;
                result.Messages.Add($"Element {element.Id.Value}: {room.Number} | {room.Name}");
                break;
            }

            if (!matchedRoom)
            {
                result.NotFoundCount++;
            }
        }

        transaction.Commit();
        return result;
    }

    private static XYZ? GetElementPoint(FamilyInstance element)
    {
        if (element.Location is LocationPoint locationPoint)
        {
            return locationPoint.Point;
        }

        BoundingBoxXYZ boundingBox = element.get_BoundingBox(null);
        return boundingBox == null ? null : (boundingBox.Min + boundingBox.Max) / 2.0;
    }
}
