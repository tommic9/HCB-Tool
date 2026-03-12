using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using HCB.RevitAddin.Features.AccessoryTerminalNumbering.Models;

namespace HCB.RevitAddin.Features.AccessoryTerminalNumbering;

public sealed class AccessoryTerminalNumberingService
{
    public IReadOnlyList<Element> CollectTargets(Document document, ICollection<ElementId> selectedIds)
    {
        if (selectedIds.Count > 0)
        {
            return selectedIds
                .Select(document.GetElement)
                .Where(IsSupported)
                .Where(element => !IsFabricAir(document, element))
                .ToList();
        }

        return new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .Where(element =>
                element.Category?.Id.Value == (long)BuiltInCategory.OST_DuctAccessory ||
                element.Category?.Id.Value == (long)BuiltInCategory.OST_PipeAccessory ||
                element.Category?.Id.Value == (long)BuiltInCategory.OST_DuctTerminal)
            .Where(element => !IsFabricAir(document, element))
            .ToList();
    }

    public IReadOnlyList<string> GetWritableStringTargetParameters(IEnumerable<Element> elements)
    {
        return elements
            .SelectMany(element => element.Parameters.Cast<Parameter>())
            .Where(parameter => !parameter.IsReadOnly && parameter.StorageType == StorageType.String)
            .Select(parameter => parameter.Definition?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .Cast<string>()
            .ToList();
    }

    public IReadOnlyList<string> GetAccessoryTypeParameterNames(Document document, IEnumerable<Element> elements)
    {
        return elements
            .Where(element =>
                element.Category?.Id.Value == (long)BuiltInCategory.OST_DuctAccessory ||
                element.Category?.Id.Value == (long)BuiltInCategory.OST_PipeAccessory)
            .Select(element => document.GetElement(element.GetTypeId()))
            .Where(typeElement => typeElement != null)
            .SelectMany(typeElement => typeElement!.Parameters.Cast<Parameter>())
            .Where(parameter => parameter.StorageType == StorageType.String)
            .Select(parameter => parameter.Definition?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .Cast<string>()
            .ToList();
    }

    public AccessoryTerminalNumberingResult Apply(Document document, IEnumerable<Element> elements, AccessoryTerminalNumberingOptions options)
    {
        Dictionary<string, SystemGroup> groups = GroupElements(document, elements);
        AccessoryTerminalNumberingResult result = new()
        {
            SystemsCount = groups.Count,
            TargetParameterName = options.TargetParameterName,
            AccessoryTypeParameterName = options.AccessoryTypeParameterName
        };

        using Transaction transaction = new(document, "MEP Item Numbering");
        transaction.Start();

        foreach (string systemAbbreviation in groups.Keys.OrderBy(key => key, StringComparer.CurrentCultureIgnoreCase))
        {
            SystemGroup group = groups[systemAbbreviation];
            int currentNumber = options.StartNumber;

            foreach (var pair in group.DuctAccessories.OrderBy(pair => pair.Key, AccessoryKeyComparer.Instance))
            {
                string accessoryTypeValue = GetAccessoryTypeValue(document, pair.Value[0], options.AccessoryTypeParameterName);
                string value = FormatAccessoryNumber(systemAbbreviation, options.DuctAccessoryPrefix, currentNumber, accessoryTypeValue);
                foreach (Element element in pair.Value)
                {
                    SetPositionNumber(element, options.TargetParameterName, value);
                }

                result.DuctAccessoryCount += pair.Value.Count;
                result.TotalCount += pair.Value.Count;
                result.SharedNumberCount += Math.Max(0, pair.Value.Count - 1);
                currentNumber++;
            }

            foreach (var pair in group.PipeAccessories.OrderBy(pair => pair.Key, AccessoryKeyComparer.Instance))
            {
                string accessoryTypeValue = GetAccessoryTypeValue(document, pair.Value[0], options.AccessoryTypeParameterName);
                string value = FormatAccessoryNumber(systemAbbreviation, options.PipeAccessoryPrefix, currentNumber, accessoryTypeValue);
                foreach (Element element in pair.Value)
                {
                    SetPositionNumber(element, options.TargetParameterName, value);
                }

                result.PipeAccessoryCount += pair.Value.Count;
                result.TotalCount += pair.Value.Count;
                result.SharedNumberCount += Math.Max(0, pair.Value.Count - 1);
                currentNumber++;
            }

            foreach (var pair in group.Terminals.OrderBy(pair => pair.Key, AirTerminalKeyComparer.Instance))
            {
                string value = $"{options.TerminalPrefix}.{currentNumber}";
                foreach (Element element in pair.Value)
                {
                    SetPositionNumber(element, options.TargetParameterName, value);
                }

                result.TerminalCount += pair.Value.Count;
                result.TotalCount += pair.Value.Count;
                result.SharedNumberCount += Math.Max(0, pair.Value.Count - 1);
                currentNumber++;
            }
        }

        transaction.Commit();
        result.Messages.Add("Pominieto typy z Manufacturer = 'FabricAir'.");
        return result;
    }

    private static bool IsSupported(Element? element)
    {
        long? categoryId = element?.Category?.Id.Value;
        return categoryId == (long)BuiltInCategory.OST_DuctAccessory ||
               categoryId == (long)BuiltInCategory.OST_PipeAccessory ||
               categoryId == (long)BuiltInCategory.OST_DuctTerminal;
    }

    private static Dictionary<string, SystemGroup> GroupElements(Document document, IEnumerable<Element> elements)
    {
        Dictionary<string, SystemGroup> groups = new(StringComparer.CurrentCultureIgnoreCase);

        foreach (Element element in elements)
        {
            string systemAbbreviation = GetSystemAbbreviation(document, element);
            if (!groups.TryGetValue(systemAbbreviation, out SystemGroup? group))
            {
                group = new SystemGroup();
                groups[systemAbbreviation] = group;
            }

            long? categoryId = element.Category?.Id.Value;
            if (categoryId == (long)BuiltInCategory.OST_DuctAccessory)
            {
                AccessoryKey key = new(GetText(element, "Size"), GetTypeName(document, element));
                group.DuctAccessories.TryAdd(key, []);
                group.DuctAccessories[key].Add(element);
            }
            else if (categoryId == (long)BuiltInCategory.OST_PipeAccessory)
            {
                AccessoryKey key = new(GetText(element, "Size"), GetTypeName(document, element));
                group.PipeAccessories.TryAdd(key, []);
                group.PipeAccessories[key].Add(element);
            }
            else if (categoryId == (long)BuiltInCategory.OST_DuctTerminal)
            {
                AirTerminalKey key = new(GetText(element, "Size"), GetTypeName(document, element), GetFlow(element, "LIN_VE_AIRFLOWRATE", 1.0));
                group.Terminals.TryAdd(key, []);
                group.Terminals[key].Add(element);
            }
        }

        return groups;
    }

    private static string GetFlow(Element element, string parameterName, double precision)
    {
        Parameter? parameter = element.LookupParameter(parameterName);
        if (parameter == null || !parameter.HasValue)
        {
            return string.Empty;
        }

        if (parameter.StorageType == StorageType.Double)
        {
            double rounded = Math.Round(parameter.AsDouble() / precision) * precision;
            return rounded.ToString("0.###");
        }

        return (parameter.AsString() ?? parameter.AsValueString() ?? string.Empty).Trim();
    }

    private static string GetTypeName(Document document, Element element)
    {
        Element? symbol = document.GetElement(element.GetTypeId());
        return (symbol?.Name ?? element.Name ?? string.Empty).Trim();
    }

    private static string GetAccessoryTypeValue(Document document, Element element, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return string.Empty;
        }

        Element? symbol = document.GetElement(element.GetTypeId());
        Parameter? parameter = symbol?.LookupParameter(parameterName);
        return (parameter?.AsString() ?? parameter?.AsValueString() ?? string.Empty).Trim();
    }

    private static string FormatAccessoryNumber(string systemAbbreviation, string prefix, int number, string typeMark)
    {
        List<string> parts = [];
        if (!string.IsNullOrWhiteSpace(systemAbbreviation))
        {
            parts.Add(systemAbbreviation);
        }

        if (!string.IsNullOrWhiteSpace(prefix))
        {
            parts.Add(prefix);
        }

        if (!string.IsNullOrWhiteSpace(typeMark))
        {
            parts.Add(typeMark);
        }

        return parts.Count == 0 ? number.ToString() : $"{string.Join(".", parts)}.{number}";
    }

    private static string GetSystemAbbreviation(Document document, Element element)
    {
        try
        {
            Parameter? instanceParameter = element.get_Parameter(BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM);
            string instanceValue = instanceParameter?.AsString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(instanceValue))
            {
                return instanceValue.Trim();
            }
        }
        catch
        {
        }

        MEPSystem? system = null;
        if (element is FamilyInstance familyInstance)
        {
            system = familyInstance.MEPModel?.ConnectorManager?.Connectors
                .Cast<Connector>()
                .Select(connector => connector.MEPSystem)
                .FirstOrDefault(candidate => candidate != null);
        }

        if (system == null)
        {
            return string.Empty;
        }

        Element? systemType = document.GetElement(system.GetTypeId());
        Parameter? abbreviationParameter = systemType?.get_Parameter(BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM);
        return (abbreviationParameter?.AsString() ?? string.Empty).Trim();
    }

    private static bool IsFabricAir(Document document, Element element)
    {
        Element? symbol = document.GetElement(element.GetTypeId());
        Parameter? manufacturerParameter = symbol?.get_Parameter(BuiltInParameter.ALL_MODEL_MANUFACTURER);
        string manufacturer = manufacturerParameter?.AsString() ?? string.Empty;
        return string.Equals(manufacturer.Trim(), "FabricAir", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetText(Element element, string parameterName)
    {
        Parameter? parameter = element.LookupParameter(parameterName);
        if (parameter == null || !parameter.HasValue)
        {
            return string.Empty;
        }

        return (parameter.AsString() ?? parameter.AsValueString() ?? string.Empty).Trim();
    }

    private static void SetPositionNumber(Element element, string targetParameterName, string value)
    {
        Parameter? parameter = element.LookupParameter(targetParameterName);
        if (parameter == null || parameter.IsReadOnly || parameter.StorageType != StorageType.String)
        {
            return;
        }

        parameter.Set(value);
    }

    private sealed class SystemGroup
    {
        public Dictionary<AccessoryKey, List<Element>> DuctAccessories { get; } = [];

        public Dictionary<AccessoryKey, List<Element>> PipeAccessories { get; } = [];

        public Dictionary<AirTerminalKey, List<Element>> Terminals { get; } = [];
    }

    private sealed record AccessoryKey(string Size, string TypeName);

    private sealed record AirTerminalKey(string Size, string TypeName, string Airflow);

    private sealed class AccessoryKeyComparer : IComparer<AccessoryKey>
    {
        public static AccessoryKeyComparer Instance { get; } = new();

        public int Compare(AccessoryKey? x, AccessoryKey? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            int sizeComparison = string.Compare(x.Size ?? string.Empty, y.Size ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);
            if (sizeComparison != 0)
            {
                return sizeComparison;
            }

            return string.Compare(x.TypeName ?? string.Empty, y.TypeName ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);
        }
    }

    private sealed class AirTerminalKeyComparer : IComparer<AirTerminalKey>
    {
        public static AirTerminalKeyComparer Instance { get; } = new();

        public int Compare(AirTerminalKey? x, AirTerminalKey? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            int sizeComparison = string.Compare(x.Size ?? string.Empty, y.Size ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);
            if (sizeComparison != 0)
            {
                return sizeComparison;
            }

            int typeComparison = string.Compare(x.TypeName ?? string.Empty, y.TypeName ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);
            if (typeComparison != 0)
            {
                return typeComparison;
            }

            return string.Compare(x.Airflow ?? string.Empty, y.Airflow ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
