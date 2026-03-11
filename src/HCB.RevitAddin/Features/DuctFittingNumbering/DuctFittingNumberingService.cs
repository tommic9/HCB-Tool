using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using HCB.RevitAddin.Features.DuctFittingNumbering.Models;

namespace HCB.RevitAddin.Features.DuctFittingNumbering;

public sealed class DuctFittingNumberingService
{
    private static readonly string[] FittingDimensionParameterNames =
    [
        "LIN_VE_DIM_A",
        "LIN_VE_DIM_B",
        "LIN_VE_DIM_C",
        "LIN_VE_DIM_D",
        "LIN_VE_DIM_E",
        "LIN_VE_DIM_F",
        "LIN_VE_DIM_H",
        "LIN_VE_DIM_L",
        "LIN_VE_DIM_M",
        "LIN_VE_DIM_N",
        "LIN_VE_DIM_R",
        "LIN_VE_DIM_R1",
        "LIN_VE_DIM_R2"
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

    public IReadOnlyList<Element> CollectTargets(Document document, ICollection<ElementId> selectedIds)
    {
        if (selectedIds.Count > 0)
        {
            return selectedIds
                .Select(document.GetElement)
                .Where(IsSupported)
                .Where(element => !ShouldSkipElement(document, element))
                .ToList();
        }

        return new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .Where(element =>
                element.Category?.Id.Value == (long)BuiltInCategory.OST_DuctCurves ||
                element.Category?.Id.Value == (long)BuiltInCategory.OST_DuctFitting)
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

    public DuctFittingNumberingResult Apply(Document document, IEnumerable<Element> elements, string targetParameterName, string lengthParameterName)
    {
        Dictionary<string, SystemGroup> groups = GroupElements(document, elements, lengthParameterName);
        DuctFittingNumberingResult result = new()
        {
            SystemsCount = groups.Count,
            LengthParameterName = lengthParameterName,
            TargetParameterName = targetParameterName
        };

        using Transaction transaction = new(document, "Duct and Fitting Numbering");
        transaction.Start();

        foreach (string systemAbbreviation in groups.Keys.OrderBy(key => key, StringComparer.CurrentCultureIgnoreCase))
        {
            SystemGroup group = groups[systemAbbreviation];
            int currentNumber = 1;

            foreach (var pair in group.Ducts.OrderBy(pair => pair.Key, DuctKeyComparer.Instance))
            {
                string value = FormatNumber(systemAbbreviation, currentNumber);
                foreach (Element element in pair.Value)
                {
                    SetPositionNumber(element, targetParameterName, value);
                }

                result.DuctCount += pair.Value.Count;
                result.TotalCount += pair.Value.Count;
                result.SharedNumberCount += Math.Max(0, pair.Value.Count - 1);
                currentNumber++;
            }

            foreach (var pair in group.Fittings.OrderBy(pair => pair.Key, FittingKeyComparer.Instance))
            {
                string value = FormatNumber(systemAbbreviation, currentNumber);
                foreach (Element element in pair.Value)
                {
                    SetPositionNumber(element, targetParameterName, value);
                }

                result.FittingCount += pair.Value.Count;
                result.TotalCount += pair.Value.Count;
                result.SharedNumberCount += Math.Max(0, pair.Value.Count - 1);
                currentNumber++;
            }
        }

        transaction.Commit();
        result.Messages.Add("Pominieto rodziny 'L_Flange_RV' oraz typy z Manufacturer = 'FabricAir'.");
        return result;
    }

    private static bool IsSupported(Element? element)
    {
        long? categoryId = element?.Category?.Id.Value;
        return categoryId == (long)BuiltInCategory.OST_DuctCurves || categoryId == (long)BuiltInCategory.OST_DuctFitting;
    }

    private static Dictionary<string, SystemGroup> GroupElements(Document document, IEnumerable<Element> elements, string lengthParameterName)
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
            if (categoryId == (long)BuiltInCategory.OST_DuctCurves)
            {
                DuctKey key = BuildDuctKey(element, lengthParameterName);
                group.Ducts.TryAdd(key, []);
                group.Ducts[key].Add(element);
            }
            else if (categoryId == (long)BuiltInCategory.OST_DuctFitting)
            {
                FittingKey key = BuildFittingKey(element);
                group.Fittings.TryAdd(key, []);
                group.Fittings[key].Add(element);
            }
        }

        return groups;
    }

    private static DuctKey BuildDuctKey(Element element, string lengthParameterName)
    {
        string size = GetText(element, "Size");
        double? length = GetLengthMillimeters(element, ResolveLengthParameterName(element, lengthParameterName), 1.0);
        return new(size, length);
    }

    private static FittingKey BuildFittingKey(Element element)
    {
        List<double?> dimensions = FittingDimensionParameterNames
            .Select(name => GetLengthMillimeters(element, name, 1.0))
            .ToList();

        return new(
            GetText(element, "Size"),
            GetText(element, "LIN_VE_DIM_TYP"),
            dimensions,
            GetAngleDegrees(element, "LIN_VE_ANG_W", 0.1));
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

    private static string GetText(Element element, string parameterName)
    {
        Parameter? parameter = element.LookupParameter(parameterName);
        if (parameter == null || !parameter.HasValue)
        {
            return string.Empty;
        }

        return (parameter.AsString() ?? parameter.AsValueString() ?? string.Empty).Trim();
    }

    private static double? GetLengthMillimeters(Element element, string parameterName, double precisionMillimeters)
    {
        Parameter? parameter = element.LookupParameter(parameterName);
        if (parameter == null || !parameter.HasValue || parameter.StorageType != StorageType.Double)
        {
            return null;
        }

        double millimeters = UnitUtils.ConvertFromInternalUnits(parameter.AsDouble(), UnitTypeId.Millimeters);
        return RoundToPrecision(millimeters, precisionMillimeters);
    }

    private static double? GetAngleDegrees(Element element, string parameterName, double precisionDegrees)
    {
        Parameter? parameter = element.LookupParameter(parameterName);
        if (parameter == null || !parameter.HasValue || parameter.StorageType != StorageType.Double)
        {
            return null;
        }

        double degrees = UnitUtils.ConvertFromInternalUnits(parameter.AsDouble(), UnitTypeId.Degrees);
        return RoundToPrecision(degrees, precisionDegrees);
    }

    private static double RoundToPrecision(double value, double precision)
    {
        return Math.Round(value / precision) * precision;
    }

    private static string FormatNumber(string prefix, int number)
    {
        return string.IsNullOrWhiteSpace(prefix) ? number.ToString() : $"{prefix}.{number}";
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
        public Dictionary<DuctKey, List<Element>> Ducts { get; } = [];

        public Dictionary<FittingKey, List<Element>> Fittings { get; } = [];
    }

    private sealed record DuctKey(string Size, double? Length);

    private sealed record FittingKey(string Size, string DimensionType, IReadOnlyList<double?> Dimensions, double? Angle);

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

            int sizeComparison = string.Compare(x.Size ?? string.Empty, y.Size ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);
            if (sizeComparison != 0)
            {
                return sizeComparison;
            }

            return Nullable.Compare(x.Length, y.Length);
        }
    }

    private sealed class FittingKeyComparer : IComparer<FittingKey>
    {
        public static FittingKeyComparer Instance { get; } = new();

        public int Compare(FittingKey? x, FittingKey? y)
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

            int typeComparison = string.Compare(x.DimensionType ?? string.Empty, y.DimensionType ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);
            if (typeComparison != 0)
            {
                return typeComparison;
            }

            int dimensionCount = Math.Max(x.Dimensions.Count, y.Dimensions.Count);
            for (int index = 0; index < dimensionCount; index++)
            {
                double? left = index < x.Dimensions.Count ? x.Dimensions[index] : null;
                double? right = index < y.Dimensions.Count ? y.Dimensions[index] : null;
                int compare = Nullable.Compare(left, right);
                if (compare != 0)
                {
                    return compare;
                }
            }

            return Nullable.Compare(x.Angle, y.Angle);
        }
    }
}
