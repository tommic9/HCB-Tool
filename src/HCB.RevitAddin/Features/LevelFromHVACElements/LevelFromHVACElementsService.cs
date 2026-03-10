using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.LevelFromHVACElements.Models;

namespace HCB.RevitAddin.Features.LevelFromHVACElements;

public sealed class LevelFromHVACElementsService
{
    private static readonly IReadOnlyDictionary<BuiltInCategory, string> CategoryLevelParameterMap = new Dictionary<BuiltInCategory, string>
    {
        [BuiltInCategory.OST_DuctTerminal] = "Level",
        [BuiltInCategory.OST_DuctAccessory] = "Level",
        [BuiltInCategory.OST_DuctFitting] = "Reference Level",
        [BuiltInCategory.OST_DuctCurves] = "Level",
        [BuiltInCategory.OST_FlexDuctCurves] = "Reference Level",
        [BuiltInCategory.OST_MechanicalEquipment] = "Level"
    };

    public bool IsSupported(Element element)
    {
        BuiltInCategory? category = GetBuiltInCategory(element);
        return category.HasValue && CategoryLevelParameterMap.ContainsKey(category.Value);
    }

    public IReadOnlyList<string> GetEditableTargetParameterNames(Element element)
    {
        return element.Parameters
            .Cast<Parameter>()
            .Where(parameter => !parameter.IsReadOnly && (parameter.StorageType == StorageType.String || parameter.StorageType == StorageType.ElementId))
            .Select(parameter => parameter.Definition.Name)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public LevelFromHvacResult Apply(Document document, IReadOnlyList<Element> elements, string targetParameterName)
    {
        LevelFromHvacResult result = new();

        using Transaction transaction = new(document, "Level From HVAC Elements");
        transaction.Start();

        foreach (Element element in elements)
        {
            if (!TryCopyLevel(element, targetParameterName))
            {
                result.FailedCount++;
                result.FailedElementIds.Add(element.Id.Value);
                continue;
            }

            result.UpdatedCount++;
        }

        transaction.Commit();
        return result;
    }

    private static bool TryCopyLevel(Element element, string targetParameterName)
    {
        BuiltInCategory? category = GetBuiltInCategory(element);
        if (!category.HasValue || !CategoryLevelParameterMap.TryGetValue(category.Value, out string? sourceParameterName))
        {
            return false;
        }

        Parameter? sourceParameter = element.LookupParameter(sourceParameterName);
        Parameter? targetParameter = element.LookupParameter(targetParameterName);
        if (sourceParameter == null || targetParameter == null || targetParameter.IsReadOnly || !sourceParameter.HasValue)
        {
            return false;
        }

        if (sourceParameter.StorageType != targetParameter.StorageType)
        {
            return false;
        }

        try
        {
            if (targetParameter.StorageType == StorageType.String)
            {
                targetParameter.Set(sourceParameter.AsString() ?? string.Empty);
                return true;
            }

            if (targetParameter.StorageType == StorageType.ElementId)
            {
                targetParameter.Set(sourceParameter.AsElementId());
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static BuiltInCategory? GetBuiltInCategory(Element element)
    {
        if (element.Category == null)
        {
            return null;
        }

        long categoryId = element.Category.Id.Value;
        return Enum.IsDefined(typeof(BuiltInCategory), (int)categoryId) ? (BuiltInCategory)(int)categoryId : null;
    }
}
