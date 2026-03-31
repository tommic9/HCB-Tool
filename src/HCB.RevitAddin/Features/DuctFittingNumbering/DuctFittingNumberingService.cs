using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using HCB.RevitAddin.Features.DuctFittingNumbering.Models;

namespace HCB.RevitAddin.Features.DuctFittingNumbering;

public sealed class DuctFittingNumberingService
{
    public enum NumberingScope
    {
        Ducts,
        Fittings,
        DuctsAndFittings
    }

    private static readonly string[] FittingGroupingParameterNames =
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
        "LIN_VE_DIM_M",
        "LIN_VE_DIM_M2",
        "LIN_VE_DIM_N",
        "LIN_VE_DIM_R",
        "LIN_VE_DIM_R1",
        "LIN_VE_DIM_R2",
        "LIN_VE_DIM_R3",
        "LIN_VE_DIM_R4",
        "LIN_VE_ANG_W"
    ];

    public IReadOnlyList<string> GetAvailableLengthParameters(Document document)
    {
        List<string> parameters = ["Length"];
        IEnumerable<Duct> ducts = new FilteredElementCollector(document)
            .OfCategory(BuiltInCategory.OST_DuctCurves)
            .WhereElementIsNotElementType()
            .Cast<Duct>()
            .Take(100);

        if (ducts.Any(duct => duct.LookupParameter("HC_Order_Length") != null))
        {
            parameters.Add("HC_Order_Length");
        }

        if (ducts.Any(duct => duct.LookupParameter("Powiekszona dlugosc") != null || duct.LookupParameter("Powiększona długość") != null))
        {
            parameters.Add("Powiększona długość");
        }

        return parameters;
    }

    public IReadOnlyList<Element> CollectTargets(Document document, ICollection<ElementId> selectedIds, NumberingScope scope = NumberingScope.DuctsAndFittings)
    {
        if (selectedIds.Count > 0)
        {
            return selectedIds
                .Select(document.GetElement)
                .Where(element => IsSupported(element, scope))
                .Where(element => !ShouldSkipElement(document, element!))
                .Cast<Element>()
                .ToList();
        }

        return new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .Where(element => IsSupported(element, scope))
            .Where(element => !ShouldSkipElement(document, element))
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

    public DuctFittingNumberingResult Apply(
        Document document,
        IEnumerable<Element> elements,
        string targetParameterName,
        string? lengthParameterName,
        bool includeSystemParameter,
        NumberingScope scope = NumberingScope.DuctsAndFittings)
    {
        List<Element> filteredElements = elements
            .Where(element => IsSupported(element, scope))
            .ToList();

        GroupedElements groups = GroupElements(document, filteredElements, lengthParameterName, scope);
        DuctFittingNumberingResult result = new()
        {
            SystemsCount = groups.SystemsCount,
            LengthParameterName = lengthParameterName ?? string.Empty,
            TargetParameterName = targetParameterName,
            IncludeSystemParameter = includeSystemParameter
        };

        using Transaction transaction = new(document, "Duct and Fitting Numbering");
        transaction.Start();

        int currentNumber = 1;

        foreach ((DuctKey _, NumberingGroup group) in groups.Ducts.OrderBy(pair => pair.Key, DuctKeyComparer.Instance))
        {
            string value = FormatNumber(group, currentNumber, includeSystemParameter);
            foreach (Element element in group.Elements)
            {
                SetPositionNumber(element, targetParameterName, value);
            }

            result.DuctCount += group.Elements.Count;
            result.TotalCount += group.Elements.Count;
            result.SharedNumberCount += Math.Max(0, group.Elements.Count - 1);
            currentNumber++;
        }

        foreach ((ComponentKey _, NumberingGroup group) in groups.Fittings.OrderBy(pair => pair.Key, ComponentKeyComparer.Instance))
        {
            string value = FormatNumber(group, currentNumber, includeSystemParameter);
            foreach (Element element in group.Elements)
            {
                SetPositionNumber(element, targetParameterName, value);
            }

            result.FittingCount += group.Elements.Count;
            result.TotalCount += group.Elements.Count;
            result.SharedNumberCount += Math.Max(0, group.Elements.Count - 1);
            currentNumber++;
        }

        foreach ((ComponentKey _, NumberingGroup group) in groups.Accessories.OrderBy(pair => pair.Key, ComponentKeyComparer.Instance))
        {
            string value = FormatNumber(group, currentNumber, includeSystemParameter);
            foreach (Element element in group.Elements)
            {
                SetPositionNumber(element, targetParameterName, value);
            }

            result.AccessoryCount += group.Elements.Count;
            result.TotalCount += group.Elements.Count;
            result.SharedNumberCount += Math.Max(0, group.Elements.Count - 1);
            currentNumber++;
        }

        transaction.Commit();
        result.Messages.Add("Pominieto rodziny 'L_Flange_RV' oraz typy z Manufacturer = 'FabricAir'.");
        result.Messages.Add("Grupy z mieszanymi System Abbreviation dostaja numer bez prefiksu systemu.");
        if (includeSystemParameter)
        {
            result.Messages.Add("Jesli grupa ma jedna wspolna wartosc HC_System, zostaje ona dodana do numeru.");
        }

        return result;
    }

    private static bool IsSupported(Element? element, NumberingScope scope)
    {
        long? categoryId = element?.Category?.Id.Value;
        return scope switch
        {
            NumberingScope.Ducts => categoryId == (long)BuiltInCategory.OST_DuctCurves,
            NumberingScope.Fittings => categoryId == (long)BuiltInCategory.OST_DuctFitting || categoryId == (long)BuiltInCategory.OST_DuctAccessory,
            _ => categoryId == (long)BuiltInCategory.OST_DuctCurves || categoryId == (long)BuiltInCategory.OST_DuctFitting || categoryId == (long)BuiltInCategory.OST_DuctAccessory
        };
    }

    private static GroupedElements GroupElements(
        Document document,
        IEnumerable<Element> elements,
        string? lengthParameterName,
        NumberingScope scope)
    {
        GroupedElements grouped = new();
        HashSet<string> systems = new(StringComparer.CurrentCultureIgnoreCase);

        foreach (Element element in elements)
        {
            string systemAbbreviation = NormalizeValue(GetSystemAbbreviation(document, element));
            string systemParameterValue = NormalizeValue(GetText(element, "HC_System"));
            if (!string.IsNullOrWhiteSpace(systemAbbreviation))
            {
                systems.Add(systemAbbreviation);
            }

            long? categoryId = element.Category?.Id.Value;
            if (categoryId == (long)BuiltInCategory.OST_DuctCurves && scope != NumberingScope.Fittings)
            {
                DuctKey key = BuildDuctKey(document, element, lengthParameterName ?? "Length");
                if (!grouped.Ducts.TryGetValue(key, out NumberingGroup? ductGroup))
                {
                    ductGroup = new NumberingGroup();
                    grouped.Ducts[key] = ductGroup;
                }

                ductGroup.Elements.Add(element);
                ductGroup.SystemAbbreviations.Add(systemAbbreviation);
                ductGroup.SystemParameterValues.Add(systemParameterValue);
            }
            else if (categoryId == (long)BuiltInCategory.OST_DuctFitting && scope != NumberingScope.Ducts)
            {
                ComponentKey key = BuildComponentKey(element, BuiltInCategory.OST_DuctFitting);
                if (!grouped.Fittings.TryGetValue(key, out NumberingGroup? fittingGroup))
                {
                    fittingGroup = new NumberingGroup();
                    grouped.Fittings[key] = fittingGroup;
                }

                fittingGroup.Elements.Add(element);
                fittingGroup.SystemAbbreviations.Add(systemAbbreviation);
                fittingGroup.SystemParameterValues.Add(systemParameterValue);
            }
            else if (categoryId == (long)BuiltInCategory.OST_DuctAccessory && scope != NumberingScope.Ducts)
            {
                ComponentKey key = BuildComponentKey(element, BuiltInCategory.OST_DuctAccessory);
                if (!grouped.Accessories.TryGetValue(key, out NumberingGroup? accessoryGroup))
                {
                    accessoryGroup = new NumberingGroup();
                    grouped.Accessories[key] = accessoryGroup;
                }

                accessoryGroup.Elements.Add(element);
                accessoryGroup.SystemAbbreviations.Add(systemAbbreviation);
                accessoryGroup.SystemParameterValues.Add(systemParameterValue);
            }
        }

        grouped.SystemsCount = systems.Count;
        return grouped;
    }

    private static DuctKey BuildDuctKey(Document document, Element element, string lengthParameterName)
    {
        string size = GetText(element, "Size");
        string typeName = GetTypeName(document, element);
        if (IsRoundElement(element))
        {
            return new(size, null, typeName, true);
        }

        double? length = GetLengthMillimeters(element, ResolveLengthParameterName(element, lengthParameterName), 1.0);
        return new(size, length, typeName, false);
    }

    private static ComponentKey BuildComponentKey(Element element, BuiltInCategory category)
    {
        List<string> values = [];
        foreach (string parameterName in FittingGroupingParameterNames)
        {
            values.Add(GetGroupingParameterValue(element, parameterName));
        }

        return new(category, values);
    }

    private static string GetGroupingParameterValue(Element element, string parameterName)
    {
        return parameterName == "LIN_VE_ANG_W"
            ? FormatNullable(GetAngleDegrees(element, parameterName, 0.1))
            : FormatNullable(GetLengthMillimeters(element, parameterName, 1.0, true));
    }

    private static string ResolveLengthParameterName(Element element, string requestedName)
    {
        if (requestedName == "Powiększona długość" && element.LookupParameter(requestedName) == null)
        {
            Parameter? fallback = element.LookupParameter("Powiekszona dlugosc");
            if (fallback != null)
            {
                return "Powiekszona dlugosc";
            }
        }

        return requestedName;
    }

    private static bool ShouldSkipElement(Document document, Element element)
    {
        return IsFlangeFamily(document, element) || IsFabricAir(document, element);
    }

    private static bool IsFlangeFamily(Document document, Element element)
    {
        try
        {
            Element? symbol = document.GetElement(element.GetTypeId());
            if (symbol is not FamilySymbol familySymbol)
            {
                return false;
            }

            return string.Equals(familySymbol.Family?.Name, "L_Flange_RV", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsFabricAir(Document document, Element element)
    {
        try
        {
            Element? symbol = document.GetElement(element.GetTypeId());
            Parameter? manufacturerParameter = symbol?.get_Parameter(BuiltInParameter.ALL_MODEL_MANUFACTURER);
            string manufacturer = manufacturerParameter?.AsString() ?? string.Empty;
            return string.Equals(manufacturer.Trim(), "FabricAir", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
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
        if (element is MEPCurve curve)
        {
            system = curve.MEPSystem;
        }

        if (system == null && element is FamilyInstance familyInstance)
        {
            ConnectorSet? connectors = familyInstance.MEPModel?.ConnectorManager?.Connectors;
            if (connectors != null)
            {
                system = connectors
                    .Cast<Connector>()
                    .Select(connector => connector.MEPSystem)
                    .FirstOrDefault(candidate => candidate != null);
            }
        }

        if (system == null)
        {
            return string.Empty;
        }

        Element? systemType = document.GetElement(system.GetTypeId());
        Parameter? abbreviationParameter = systemType?.get_Parameter(BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM);
        return (abbreviationParameter?.AsString() ?? string.Empty).Trim();
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

    private static string GetTypeName(Document document, Element element)
    {
        Element? symbol = document.GetElement(element.GetTypeId());
        return (symbol?.Name ?? element.Name ?? string.Empty).Trim();
    }

    private static bool IsRoundElement(Element element)
    {
        ConnectorSet? connectors = GetConnectors(element);
        if (connectors == null)
        {
            return false;
        }

        bool hasAnyConnector = false;
        foreach (Connector connector in connectors)
        {
            hasAnyConnector = true;
            if (connector.Shape != ConnectorProfileType.Round)
            {
                return false;
            }
        }

        return hasAnyConnector;
    }

    private static ConnectorSet? GetConnectors(Element element)
    {
        if (element is MEPCurve mepCurve)
        {
            return mepCurve.ConnectorManager?.Connectors;
        }

        if (element is FamilyInstance familyInstance)
        {
            return familyInstance.MEPModel?.ConnectorManager?.Connectors;
        }

        return null;
    }

    private static double? GetLengthMillimeters(Element element, string parameterName, double precisionMillimeters, bool allowTextFallback = false)
    {
        Parameter? parameter = element.LookupParameter(parameterName);
        if (parameter == null || !parameter.HasValue)
        {
            return null;
        }

        if (parameter.StorageType == StorageType.Double)
        {
            double millimeters = UnitUtils.ConvertFromInternalUnits(parameter.AsDouble(), UnitTypeId.Millimeters);
            return RoundToPrecision(millimeters, precisionMillimeters);
        }

        if (allowTextFallback)
        {
            string text = (parameter.AsString() ?? parameter.AsValueString() ?? string.Empty).Trim();
            if (double.TryParse(text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
            {
                return RoundToPrecision(parsed, precisionMillimeters);
            }
        }

        return null;
    }

    private static double? GetAngleDegrees(Element element, string parameterName, double precisionDegrees)
    {
        Parameter? parameter = element.LookupParameter(parameterName);
        if (parameter == null || !parameter.HasValue)
        {
            return null;
        }

        if (parameter.StorageType == StorageType.Double)
        {
            double degrees = UnitUtils.ConvertFromInternalUnits(parameter.AsDouble(), UnitTypeId.Degrees);
            return RoundToPrecision(degrees, precisionDegrees);
        }

        string text = (parameter.AsString() ?? parameter.AsValueString() ?? string.Empty).Trim();
        if (double.TryParse(text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
        {
            return RoundToPrecision(parsed, precisionDegrees);
        }

        return null;
    }

    private static double RoundToPrecision(double value, double precision)
    {
        return Math.Round(value / precision) * precision;
    }

    private static string NormalizeValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string FormatNullable(double? value)
    {
        return value?.ToString("0.###") ?? string.Empty;
    }

    private static string FormatNumber(NumberingGroup group, int number, bool includeSystemParameter)
    {
        List<string> parts = [];

        if (includeSystemParameter)
        {
            List<string> distinctSystemParameters = group.SystemParameterValues
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            if (distinctSystemParameters.Count == 1)
            {
                parts.Add(distinctSystemParameters[0]);
            }
        }

        List<string> distinctAbbreviations = group.SystemAbbreviations
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (distinctAbbreviations.Count == 1)
        {
            parts.Add(distinctAbbreviations[0]);
        }

        parts.Add(number.ToString());
        return string.Join('.', parts);
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

    private sealed class GroupedElements
    {
        public Dictionary<DuctKey, NumberingGroup> Ducts { get; } = [];

        public Dictionary<ComponentKey, NumberingGroup> Fittings { get; } = [];

        public Dictionary<ComponentKey, NumberingGroup> Accessories { get; } = [];

        public int SystemsCount { get; set; }
    }

    private sealed class NumberingGroup
    {
        public List<Element> Elements { get; } = [];

        public HashSet<string> SystemAbbreviations { get; } = new(StringComparer.CurrentCultureIgnoreCase);

        public HashSet<string> SystemParameterValues { get; } = new(StringComparer.CurrentCultureIgnoreCase);
    }

    private sealed record DuctKey(string Size, double? Length, string TypeName, bool IsRound);

    private sealed record ComponentKey(BuiltInCategory Category, IReadOnlyList<string> Values);

    private sealed class DuctKeyComparer : IComparer<DuctKey>
    {
        public static DuctKeyComparer Instance { get; } = new();

        public int Compare(DuctKey? x, DuctKey? y)
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

            int roundComparison = x.IsRound.CompareTo(y.IsRound);
            if (roundComparison != 0)
            {
                return roundComparison;
            }

            int typeComparison = string.Compare(x.TypeName ?? string.Empty, y.TypeName ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);
            if (typeComparison != 0)
            {
                return typeComparison;
            }

            int sizeComparison = string.Compare(x.Size ?? string.Empty, y.Size ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);
            if (sizeComparison != 0)
            {
                return sizeComparison;
            }

            return Nullable.Compare(x.Length, y.Length);
        }
    }

    private sealed class ComponentKeyComparer : IComparer<ComponentKey>
    {
        public static ComponentKeyComparer Instance { get; } = new();

        public int Compare(ComponentKey? x, ComponentKey? y)
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

            int categoryComparison = x.Category.CompareTo(y.Category);
            if (categoryComparison != 0)
            {
                return categoryComparison;
            }

            int count = Math.Max(x.Values.Count, y.Values.Count);
            for (int index = 0; index < count; index++)
            {
                string left = index < x.Values.Count ? x.Values[index] ?? string.Empty : string.Empty;
                string right = index < y.Values.Count ? y.Values[index] ?? string.Empty : string.Empty;
                int compare = string.Compare(left, right, StringComparison.CurrentCultureIgnoreCase);
                if (compare != 0)
                {
                    return compare;
                }
            }

            return 0;
        }
    }
}
