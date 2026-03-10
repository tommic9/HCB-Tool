using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.MassOfDuctsFittings.Models;

namespace HCB.RevitAddin.Features.MassOfDuctsFittings;

public sealed class MassOfDuctsFittingsService
{
    private const double SteelDensity = 7850.0;
    private const double SheetThicknessMeters = 0.001;
    private const double FrameThicknessMeters = 0.002;

    private static readonly BuiltInCategory[] AllowedCategories =
    [
        BuiltInCategory.OST_DuctCurves,
        BuiltInCategory.OST_DuctFitting,
        BuiltInCategory.OST_FlexDuctCurves,
        BuiltInCategory.OST_DuctAccessory
    ];

    private static readonly IReadOnlyDictionary<int, int> FrameWidthByDimensionMillimeters = new Dictionary<int, int>
    {
        [500] = 20,
        [1000] = 30,
        [1500] = 40,
        [999999] = 50
    };

    private static readonly IReadOnlyDictionary<int, double> RoundDuctWeights = new Dictionary<int, double>
    {
        [63] = 0.85, [80] = 0.82, [100] = 1.02, [112] = 1.14, [125] = 1.28, [140] = 1.43,
        [150] = 1.53, [160] = 1.64, [180] = 2.04, [200] = 2.27, [224] = 2.54, [250] = 2.84,
        [280] = 3.58, [300] = 3.83, [315] = 4.02, [355] = 4.54, [400] = 6.01, [450] = 7.03,
        [500] = 7.81, [560] = 8.74, [600] = 9.37, [630] = 9.84, [710] = 13.1, [800] = 14.8,
        [900] = 21.7, [1000] = 24.1, [1120] = 27.0, [1250] = 30.2, [1400] = 47.5, [1500] = 50.9,
        [1600] = 54.3, [1800] = 63.1, [2000] = 71.9
    };

    public MassOfDuctsFittingsResult Apply(Document document, ICollection<ElementId> selectedElementIds)
    {
        var categoryFilters = AllowedCategories.Select(category => new ElementCategoryFilter(category)).Cast<ElementFilter>().ToList();
        ElementFilter filter = new LogicalOrFilter(categoryFilters);
        HashSet<long> selectedIds = selectedElementIds.Select(id => id.Value).ToHashSet();
        bool useSelection = selectedIds.Count > 0;

        List<Element> elements = new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .WherePasses(filter)
            .ToElements()
            .Where(element => !useSelection || selectedIds.Contains(element.Id.Value))
            .ToList();

        MassOfDuctsFittingsResult result = new();

        using Transaction transaction = new(document, "Mass Of Ducts & Fittings");
        transaction.Start();

        foreach (Element element in elements)
        {
            if (!TryCalculateMass(element, out double mass, out string reason))
            {
                result.SkippedCount++;
                result.Messages.Add($"Element {element.Id.Value}: {reason}");
                continue;
            }

            Parameter? massParameter = element.LookupParameter("HC_Masa");
            if (massParameter == null || massParameter.IsReadOnly || massParameter.StorageType != StorageType.Double)
            {
                result.SkippedCount++;
                result.Messages.Add($"Element {element.Id.Value}: brak parametru HC_Masa.");
                continue;
            }

            massParameter.Set(mass);
            result.UpdatedCount++;
        }

        transaction.Commit();
        return result;
    }

    private static bool TryCalculateMass(Element element, out double mass, out string reason)
    {
        mass = 0;
        reason = string.Empty;

        long? categoryId = element.Category?.Id.Value;
        if (!categoryId.HasValue)
        {
            reason = "Brak kategorii.";
            return false;
        }

        GetProfileData(element, out double maxDimensionInternal, out double perimeterInternal, out int connectorCount, out string shape);

        if (categoryId.Value == (int)BuiltInCategory.OST_DuctCurves && shape == "round")
        {
            Parameter? lengthParameter = element.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
            if (lengthParameter?.StorageType != StorageType.Double)
            {
                reason = "Brak parametru dlugosci.";
                return false;
            }

            int diameterMillimeters = (int)Math.Round(UnitUtils.ConvertFromInternalUnits(maxDimensionInternal, UnitTypeId.Millimeters));
            if (!RoundDuctWeights.TryGetValue(diameterMillimeters, out double weightPerMeter))
            {
                reason = $"Brak wagi dla srednicy {diameterMillimeters} mm.";
                return false;
            }

            double lengthMeters = UnitUtils.ConvertFromInternalUnits(lengthParameter.AsDouble(), UnitTypeId.Meters);
            mass = lengthMeters * weightPerMeter;
            return true;
        }

        if (categoryId.Value == (int)BuiltInCategory.OST_DuctFitting && shape == "round")
        {
            reason = "Okragla ksztaltka nie jest jeszcze obslugiwana.";
            return false;
        }

        Parameter? areaParameter = element.LookupParameter("HC_Area");
        if (areaParameter?.StorageType != StorageType.Double)
        {
            reason = "Brak parametru HC_Area.";
            return false;
        }

        double areaSquareMeters = UnitUtils.ConvertFromInternalUnits(areaParameter.AsDouble(), UnitTypeId.SquareMeters);
        if (areaSquareMeters <= 0)
        {
            reason = "HC_Area <= 0.";
            return false;
        }

        double maxDimensionMillimeters = UnitUtils.ConvertFromInternalUnits(maxDimensionInternal, UnitTypeId.Millimeters);
        double perimeterMeters = UnitUtils.ConvertFromInternalUnits(perimeterInternal, UnitTypeId.Meters);
        double frameWidthMeters = GetFrameWidthMillimeters(maxDimensionMillimeters) / 1000.0;
        double frameMass = perimeterMeters * frameWidthMeters * FrameThicknessMeters * SteelDensity * connectorCount;
        double sheetMass = areaSquareMeters * SheetThicknessMeters * SteelDensity;
        mass = sheetMass + frameMass;
        return true;
    }

    private static void GetProfileData(Element element, out double maxDimension, out double perimeter, out int connectorCount, out string shape)
    {
        maxDimension = 0;
        perimeter = 0;
        connectorCount = 0;
        shape = "unknown";

        ConnectorSet? connectors = GetConnectors(element);
        if (connectors == null)
        {
            return;
        }

        List<double> widths = [];
        List<double> heights = [];
        List<double> diameters = [];

        foreach (Connector connector in connectors)
        {
            connectorCount++;
            try
            {
                if (connector.Shape == ConnectorProfileType.Round)
                {
                    diameters.Add(connector.Radius * 2.0);
                }
                else
                {
                    widths.Add(connector.Width);
                    heights.Add(connector.Height);
                }
            }
            catch
            {
            }
        }

        if (widths.Count > 0 && heights.Count > 0)
        {
            double width = widths.Max();
            double height = heights.Max();
            maxDimension = Math.Max(width, height);
            perimeter = 2.0 * (width + height);
            shape = "rect";
            return;
        }

        if (diameters.Count > 0)
        {
            double diameter = diameters.Max();
            maxDimension = diameter;
            perimeter = Math.PI * diameter;
            shape = "round";
        }
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

    private static int GetFrameWidthMillimeters(double maxDimensionMillimeters)
    {
        foreach (var pair in FrameWidthByDimensionMillimeters.OrderBy(pair => pair.Key))
        {
            if (maxDimensionMillimeters <= pair.Key)
            {
                return pair.Value;
            }
        }

        return FrameWidthByDimensionMillimeters.Values.Last();
    }
}
