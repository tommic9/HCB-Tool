using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.NumberingSystemElements.Models;

namespace HCB.RevitAddin.Features.NumberingSystemElements;

public sealed class NumberingSystemElementsService
{
    private const string TargetParameterName = "LIN_POSITION_NUMBER_A";
    private const string SystemAbbreviationParameterName = "System Abbreviation";

    private static readonly string[] DimensionParameterNames =
    [
        "LIN_VE_DIM_TYP",
        "LIN_VE_DIM_L",
        "LIN_VE_DIM_A",
        "LIN_VE_DIM_B",
        "LIN_VE_DIM_C",
        "LIN_VE_DIM_D",
        "LIN_VE_DIM_E",
        "LIN_VE_DIM_F",
        "LIN_VE_DIM_H",
        "LIN_VE_DIM_M1",
        "LIN_VE_DIM_M2",
        "LIN_VE_DIM_N",
        "LIN_VE_DIM_R",
        "LIN_VE_DIM_R1",
        "LIN_VE_DIM_R2",
        "LIN_VE_DIM_R3",
        "LIN_VE_DIM_R4"
    ];

    public NumberingSystemElementsResult Apply(Document document, ElementId startElementId)
    {
        NumberingSystemElementsResult result = new();
        Element? startElement = document.GetElement(startElementId);
        if (startElement == null)
        {
            result.Messages.Add("Nie znaleziono elementu startowego.");
            return result;
        }

        List<Element> orderedElements = TraverseSystem(startElement);
        if (orderedElements.Count == 0)
        {
            result.Messages.Add("Nie znaleziono polaczonych kanalow ani ksztaltek.");
            return result;
        }

        Dictionary<string, int> groupNumbers = new();

        using Transaction transaction = new(document, "Numbering System Elements");
        transaction.Start();

        foreach (Element element in orderedElements)
        {
            string groupKey = GetGroupKey(element);
            if (string.IsNullOrWhiteSpace(groupKey))
            {
                continue;
            }

            if (!groupNumbers.TryGetValue(groupKey, out int groupNumber))
            {
                groupNumber = groupNumbers.Count + 1;
                groupNumbers[groupKey] = groupNumber;
            }

            Parameter? targetParameter = element.LookupParameter(TargetParameterName);
            if (targetParameter == null || targetParameter.IsReadOnly || targetParameter.StorageType != StorageType.String)
            {
                result.Messages.Add($"Element {element.Id.Value}: brak {TargetParameterName}.");
                continue;
            }

            string systemAbbreviation = element.LookupParameter(SystemAbbreviationParameterName)?.AsString()
                ?? element.LookupParameter(SystemAbbreviationParameterName)?.AsValueString()
                ?? string.Empty;

            targetParameter.Set($"{systemAbbreviation}{groupNumber}");
            result.UpdatedCount++;
        }

        transaction.Commit();
        result.GroupCount = groupNumbers.Count;
        if (result.Messages.Count == 0)
        {
            result.Messages.Add("Numeracja zakonczona bez bledow.");
        }

        return result;
    }

    private static List<Element> TraverseSystem(Element startElement)
    {
        Queue<Element> queue = new();
        HashSet<long> visited = [];
        List<Element> ordered = [];
        queue.Enqueue(startElement);

        while (queue.Count > 0)
        {
            Element element = queue.Dequeue();
            if (!visited.Add(element.Id.Value))
            {
                continue;
            }

            long? categoryId = element.Category?.Id.Value;
            if (categoryId == (int)BuiltInCategory.OST_DuctFitting || categoryId == (int)BuiltInCategory.OST_DuctCurves)
            {
                ordered.Add(element);
            }

            foreach (Element neighbor in GetNeighbors(element))
            {
                if (!visited.Contains(neighbor.Id.Value))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        return ordered;
    }

    private static IEnumerable<Element> GetNeighbors(Element element)
    {
        ConnectorSet? connectors = GetConnectors(element);
        if (connectors == null)
        {
            yield break;
        }

        long? categoryId = element.Category?.Id.Value;
        foreach (Connector connector in connectors)
        {
            if (categoryId == (int)BuiltInCategory.OST_DuctFitting && connector.Shape != ConnectorProfileType.Rectangular)
            {
                continue;
            }

            foreach (Connector reference in connector.AllRefs)
            {
                Element owner = reference.Owner;
                long? ownerCategoryId = owner.Category?.Id.Value;
                if ((ownerCategoryId == (int)BuiltInCategory.OST_DuctFitting || ownerCategoryId == (int)BuiltInCategory.OST_DuctCurves)
                    && owner.Id != element.Id)
                {
                    yield return owner;
                }
            }
        }
    }

    private static ConnectorSet? GetConnectors(Element element)
    {
        if (element is MEPCurve curve)
        {
            return curve.ConnectorManager?.Connectors;
        }

        if (element is FamilyInstance familyInstance)
        {
            return familyInstance.MEPModel?.ConnectorManager?.Connectors;
        }

        return null;
    }

    private static string GetGroupKey(Element element)
    {
        long? categoryId = element.Category?.Id.Value;
        if (categoryId == (int)BuiltInCategory.OST_DuctFitting)
        {
            return string.Join("|", DimensionParameterNames.Select(name =>
            {
                Parameter? parameter = element.LookupParameter(name);
                return parameter?.AsValueString() ?? parameter?.AsString() ?? string.Empty;
            }));
        }

        if (categoryId == (int)BuiltInCategory.OST_DuctCurves)
        {
            string size = element.LookupParameter("Size")?.AsValueString()
                ?? element.get_Parameter(BuiltInParameter.RBS_CALCULATED_SIZE)?.AsString()
                ?? string.Empty;
            string length = element.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH)?.AsValueString() ?? string.Empty;
            return $"{size}|{length}";
        }

        return string.Empty;
    }
}
