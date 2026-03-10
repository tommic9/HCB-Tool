using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using HCB.RevitAddin.Features.DuctFittingsArea.Models;

namespace HCB.RevitAddin.Features.DuctFittingsArea;

public sealed class DuctFittingsAreaService
{
    private static readonly string[] LinearDimensions = { "A", "B", "C", "D", "E", "F", "H", "L", "M", "M2", "N", "R", "R1", "R2", "R3", "R4", "T" };
    private static readonly string[] MagicadDimensions = { "a", "b", "c", "d", "e", "f", "h", "l", "m", "m2", "n", "r", "r1", "r2", "r3", "r4", "t" };

    private static readonly IReadOnlyDictionary<string, string[]> LinearRequiredParams = new Dictionary<string, string[]>
    {
        ["BA"] = new[] { "A", "B", "D", "E", "F", "R", "LIN_VE_ANG_W" },
        ["BO"] = new[] { "A", "B", "E" },
        ["BS"] = new[] { "A", "B", "D", "E", "F", "R", "LIN_VE_ANG_W" },
        ["ES"] = new[] { "L", "A", "B", "D", "E", "R" },
        ["OA"] = new[] { "L", "A", "B", "C", "D", "E", "F", "M", "T" },
        ["OS"] = new[] { "L", "A", "B", "C", "D", "E", "F", "M", "T" },
        ["RA"] = new[] { "L", "A", "B", "C", "D", "E", "F", "M", "T" },
        ["RS"] = new[] { "L", "A", "B", "C", "D", "E", "F", "M", "T" },
        ["SU"] = new[] { "L", "A", "B", "D", "R" },
        ["TD"] = new[] { "L", "A", "B", "C", "D", "H", "R" },
        ["TG"] = new[] { "L", "A", "B", "D", "H", "M", "N", "R1", "R2" },
        ["UA"] = new[] { "L", "A", "B", "C", "D", "E", "F" },
        ["US"] = new[] { "L", "A", "B", "C", "D", "E", "F" },
        ["WA"] = new[] { "A", "B", "D", "E", "F", "R", "LIN_VE_ANG_W" },
        ["WS"] = new[] { "A", "B", "D", "E", "F", "R", "LIN_VE_ANG_W" },
        ["HS"] = new[] { "A", "B", "D", "E", "M", "L", "H" }
    };

    private static readonly IReadOnlyDictionary<string, string[]> MagicadRequiredParams = new Dictionary<string, string[]>
    {
        ["BA"] = new[] { "a", "b", "d", "e", "f", "r", "Alpha" },
        ["BO"] = new[] { "a", "b" },
        ["BS"] = new[] { "a", "b", "e", "f", "r", "Alpha" },
        ["ES"] = new[] { "l", "a", "b", "e" },
        ["OA"] = new[] { "l", "a", "b", "c", "d", "e", "f" },
        ["OS"] = new[] { "l", "a", "b", "c", "d", "e", "f" },
        ["RA"] = new[] { "l", "a", "b", "d", "e", "f" },
        ["RS"] = new[] { "a", "b", "d", "e", "f" },
        ["SU"] = new[] { "l", "a", "b", "d", "r" },
        ["SU-Fase"] = new[] { "RLT_DIN_l", "a", "b", "d" },
        ["TD"] = new[] { "l", "a", "b", "c", "d", "h", "r" },
        ["TG"] = new[] { "l", "a", "b", "d", "h", "m" },
        ["UA"] = new[] { "l", "a", "b", "c", "d", "e" },
        ["US"] = new[] { "l", "a", "b", "c", "d", "e", "f" },
        ["WA"] = new[] { "a", "b", "d", "e", "f", "r", "Alpha" },
        ["WS"] = new[] { "a", "b", "d", "e", "f", "r", "Alpha" },
        ["HS"] = new[] { "a", "b", "d", "e", "m", "l", "h" }
    };

    public DuctFittingsAreaResult CalculateLinear(Document document, DuctFittingsAreaOptions options)
    {
        DuctFittingProfile profile = new(
            "LIN_VE_DIM_",
            "LIN_VE_DIM_TYP",
            "LIN_VE_ANG_W",
            LinearDimensions,
            LinearRequiredParams);

        return Calculate(document, profile, options, CalculateLinearDimensions);
    }

    public DuctFittingsAreaResult CalculateMagicad(Document document, DuctFittingsAreaOptions options)
    {
        DuctFittingProfile profile = new(
            "DIN_",
            "RLT_DIN_KZ",
            "Alpha",
            MagicadDimensions,
            MagicadRequiredParams,
            "RLT_DIN_l");

        return Calculate(document, profile, options, CalculateMagicadDimensions);
    }

    private DuctFittingsAreaResult Calculate(
        Document document,
        DuctFittingProfile profile,
        DuctFittingsAreaOptions options,
        Func<string, Dictionary<string, double>, (double Length, double Perimeter)> calculator)
    {
        List<FamilyInstance> fittings = new FilteredElementCollector(document)
            .OfCategory(BuiltInCategory.OST_DuctFitting)
            .WhereElementIsNotElementType()
            .OfType<FamilyInstance>()
            .Where(fitting => fitting.LookupParameter("Size")?.AsString()?.Contains("x", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        DuctFittingsAreaResult result = new();

        using Transaction transaction = new(document, "Duct Fittings Area");
        transaction.Start();

        CopyDuctAreas(document, result, options);

        foreach (FamilyInstance fitting in fittings)
        {
            result.ProcessedCount++;

            string? dimensionType = fitting.LookupParameter(profile.DimensionTypeParameter)?.AsString();
            if (string.IsNullOrWhiteSpace(dimensionType) || !profile.RequiredParams.TryGetValue(dimensionType, out string[]? requiredParams))
            {
                result.SkippedCount++;
                continue;
            }

            Dictionary<string, double> values = ReadParameterValues(fitting, profile);
            if (requiredParams.Any(required => !values.ContainsKey(required)))
            {
                result.SkippedCount++;
                result.Messages.Add($"Pominięto {fitting.Id.Value}: brak wymaganych parametrów dla typu {dimensionType}.");
                continue;
            }

            try
            {
                (double length, double perimeter) = calculator(dimensionType, values);
                double lengthMeters = UnitUtils.ConvertFromInternalUnits(length, UnitTypeId.Meters);
                double perimeterMeters = UnitUtils.ConvertFromInternalUnits(perimeter, UnitTypeId.Meters);
                double area = Math.Round(lengthMeters * perimeterMeters, 2);
                if (options.ClampValuesBelowOneToOne && area < 1.0)
                {
                    area = 1.0;
                }

                Parameter areaParameter = fitting.LookupParameter("HC_Area");
                if (areaParameter == null || areaParameter.IsReadOnly)
                {
                    result.SkippedCount++;
                    result.Messages.Add($"Pominięto {fitting.Id.Value}: brak parametru HC_Area.");
                    continue;
                }

                SetAreaParameterValue(areaParameter, area);
                result.UpdatedCount++;
                result.Messages.Add($"Element {fitting.Id.Value}: HC_Area = {area:0.00}");
            }
            catch (Exception ex)
            {
                result.SkippedCount++;
                result.Messages.Add($"Pominięto {fitting.Id.Value}: {ex.Message}");
            }
        }

        transaction.Commit();
        return result;
    }

    private static void CopyDuctAreas(Document document, DuctFittingsAreaResult result, DuctFittingsAreaOptions options)
    {
        List<Autodesk.Revit.DB.Mechanical.Duct> ducts = new FilteredElementCollector(document)
            .OfCategory(BuiltInCategory.OST_DuctCurves)
            .WhereElementIsNotElementType()
            .OfType<Autodesk.Revit.DB.Mechanical.Duct>()
            .ToList();

        foreach (Autodesk.Revit.DB.Mechanical.Duct duct in ducts)
        {
            result.ProcessedCount++;

            Parameter areaParameter = duct.LookupParameter("HC_Area");
            if (areaParameter == null || areaParameter.IsReadOnly)
            {
                result.SkippedCount++;
                result.Messages.Add($"Pominieto kanal {duct.Id.Value}: brak parametru HC_Area.");
                continue;
            }

            Parameter sourceAreaParameter = duct.get_Parameter(BuiltInParameter.RBS_CURVE_SURFACE_AREA);
            if (sourceAreaParameter == null || sourceAreaParameter.StorageType != StorageType.Double)
            {
                result.SkippedCount++;
                result.Messages.Add($"Pominieto kanal {duct.Id.Value}: brak parametru powierzchni.");
                continue;
            }

            double areaSquareMeters = UnitUtils.ConvertFromInternalUnits(sourceAreaParameter.AsDouble(), UnitTypeId.SquareMeters);
            double roundedArea = Math.Round(areaSquareMeters, 3);
            if (options.ClampValuesBelowOneToOne && roundedArea < 1.0)
            {
                roundedArea = 1.0;
            }

            SetAreaParameterValue(areaParameter, roundedArea);
            result.UpdatedCount++;
            result.Messages.Add($"Kanal {duct.Id.Value}: HC_Area = {roundedArea:0.###}");
        }
    }

    private static Dictionary<string, double> ReadParameterValues(FamilyInstance fitting, DuctFittingProfile profile)
    {
        Dictionary<string, double> values = new(StringComparer.OrdinalIgnoreCase);

        foreach (string dimension in profile.Dimensions)
        {
            Parameter parameter = fitting.LookupParameter(profile.DimensionPrefix + dimension);
            if (parameter?.StorageType == StorageType.Double)
            {
                values[dimension] = parameter.AsDouble();
            }
        }

        Parameter angleParameter = fitting.LookupParameter(profile.AngleParameter);
        if (angleParameter?.StorageType == StorageType.Double)
        {
            values[profile.AngleParameter] = angleParameter.AsDouble();
        }

        if (!string.IsNullOrWhiteSpace(profile.ExtraDoubleParameter))
        {
            Parameter extraParameter = fitting.LookupParameter(profile.ExtraDoubleParameter);
            if (extraParameter?.StorageType == StorageType.Double)
            {
                values[profile.ExtraDoubleParameter] = extraParameter.AsDouble();
            }
        }

        return values;
    }

    private static void SetAreaParameterValue(Parameter areaParameter, double squareMetersValue)
    {
        if (areaParameter.StorageType != StorageType.Double)
        {
            return;
        }

        ForgeTypeId dataType = areaParameter.Definition.GetDataType();
        if (dataType == SpecTypeId.Area)
        {
            double internalArea = UnitUtils.ConvertToInternalUnits(squareMetersValue, UnitTypeId.SquareMeters);
            areaParameter.Set(internalArea);
            return;
        }

        areaParameter.Set(squareMetersValue);
    }

    private static (double Length, double Perimeter) CalculateLinearDimensions(string type, Dictionary<string, double> p)
    {
        return type switch
        {
            "BS" => (p["LIN_VE_ANG_W"] * (p["R"] + p["B"]) + p["E"] + p["F"], 2 * (p["A"] + p["B"])),
            "BO" => (((2 * (p["A"] + p["B"])) * p["E"]) + (p["A"] * p["B"]), 1),
            "BA" => (p["LIN_VE_ANG_W"] * (p["R"] + p["B"]) + p["E"] + p["F"], 2 * (p["A"] + Math.Max(p["B"], p["D"]))),
            "WS" => (2 * p["B"] + p["E"] + p["F"], 2 * (p["A"] + p["B"])),
            "WA" => (p["B"] + p["D"] + p["E"] + p["F"], 2 * (p["A"] + Math.Max(p["B"], p["D"]))),
            "US" => (Math.Sqrt(Math.Pow(p["L"], 2) + Math.Pow(p["A"] + p["B"] >= p["C"] + p["D"] ? p["E"] : p["F"], 2)), 2 * (p["A"] + p["B"] >= p["C"] + p["D"] ? p["A"] + p["B"] : p["C"] + p["D"])),
            "UA" => (Math.Sqrt(Math.Pow(p["L"], 2) + Math.Pow(p["A"] + p["B"] >= p["C"] + p["D"] ? p["B"] - p["D"] + p["E"] : p["E"], 2)), 2 * (p["A"] + p["B"] >= p["C"] + p["D"] ? p["A"] + p["B"] : p["C"] + p["D"])),
            "OS" => (Math.Sqrt(Math.Pow(p["L"], 2) + Math.Pow(p["A"] + p["B"] >= 2 * Math.PI * Math.Sqrt((2 * p["D"] + 2 * p["C"]) / 2) ? p["E"] : p["F"], 2)), p["A"] + p["B"] >= 2 * Math.PI * Math.Sqrt((2 * p["D"] + 2 * p["C"]) / 2) ? 2 * (p["A"] + p["B"]) : 2 * Math.PI * Math.Sqrt((2 * p["D"] + 2 * p["C"]) / 2)),
            "OA" => CalculateLinearOvalA(p),
            "RS" => CalculateLinearRoundS(p),
            "RA" => CalculateLinearRoundA(p),
            "ES" => (Math.Sqrt(Math.Pow(p["L"], 2) + Math.Pow(p["E"], 2)), 2 * (p["A"] + p["B"])),
            "EA" => (Math.Sqrt(Math.Pow(p["L"], 2) + Math.Pow(p["B"] >= p["D"] ? p["B"] - p["D"] + p["E"] : p["E"], 2)), 2 * (p["B"] >= p["D"] ? p["A"] + p["B"] : p["C"] + p["D"])),
            "TG" => CalculateLinearTg(p),
            "TA" => CalculateLinearTa(p),
            "HS" => CalculateLinearHs(p),
            "SU" => (Math.Sqrt(Math.Pow(p["L"], 2) + Math.Pow(p["R"], 2)), 2 * (p["A"] + p["B"])),
            "TD" => (Math.Sqrt(Math.Pow(p["L"], 2) + Math.Pow(p["R"], 2)), 2 * (p["A"] + p["B"])),
            _ => throw new InvalidOperationException($"Nieznany typ kształtki: {type}")
        };
    }

    private static (double Length, double Perimeter) CalculateMagicadDimensions(string type, Dictionary<string, double> p)
    {
        return type switch
        {
            "BS" => (p["Alpha"] * (p["r"] + p["b"]) + p["e"] + p["f"], 2 * (p["a"] + p["b"])),
            "BO" => (1, 2 * (p["a"] + p["b"])),
            "BA" => (p["Alpha"] * (p["r"] + p["b"]) + p["e"] + p["f"], 2 * (p["a"] + Math.Max(p["b"], p["d"]))),
            "WS" => (2 * p["b"] + p["e"] + p["f"], 2 * (p["a"] + p["b"])),
            "WA" => (p["b"] + p["d"] + p["e"] + p["f"], 2 * (p["a"] + Math.Max(p["b"], p["d"]))),
            "US" => (Math.Sqrt(Math.Pow(p["l"], 2) + Math.Pow(p["a"] + p["b"] >= p["c"] + p["d"] ? p["e"] : p["f"], 2)), 2 * (p["a"] + p["b"] >= p["c"] + p["d"] ? p["a"] + p["b"] : p["c"] + p["d"])),
            "SU-Fase" => (Math.Sqrt(p["RLT_DIN_l"]), 2 * (p["a"] + p["b"] >= p["c"] + p["d"] ? p["a"] + p["b"] : p["c"] + p["d"])),
            "UA" => (Math.Sqrt(Math.Pow(p["l"], 2) + Math.Pow(p["a"] + p["b"] >= p["c"] + p["d"] ? p["b"] - p["d"] + p["e"] : p["e"], 2)), 2 * (p["a"] + p["b"] >= p["c"] + p["d"] ? p["a"] + p["b"] : p["c"] + p["d"])),
            "OS" => (Math.Sqrt(Math.Pow(p["l"], 2) + Math.Pow(p["a"] + p["b"] >= 2 * Math.PI * Math.Sqrt((2 * p["d"] + 2 * p["c"]) / 2) ? p["e"] : p["f"], 2)), p["a"] + p["b"] >= 2 * Math.PI * Math.Sqrt((2 * p["d"] + 2 * p["c"]) / 2) ? 2 * (p["a"] + p["b"]) : 2 * Math.PI * Math.Sqrt((2 * p["d"] + 2 * p["c"]) / 2)),
            "OA" => CalculateMagicadOvalA(p),
            "RS" => CalculateMagicadRoundS(p),
            "RA" => CalculateMagicadRoundA(p),
            "ES" => (Math.Sqrt(Math.Pow(p["l"], 2) + Math.Pow(p["e"], 2)), 2 * (p["a"] + p["b"])),
            "EA" => (Math.Sqrt(Math.Pow(p["l"], 2) + Math.Pow(p["b"] >= p["d"] ? p["b"] - p["d"] + p["e"] : p["e"], 2)), 2 * (p["b"] >= p["d"] ? p["a"] + p["b"] : p["c"] + p["d"])),
            "TG" => CalculateMagicadTg(p),
            "TA" => CalculateMagicadTa(p),
            "HS" => CalculateMagicadHs(p),
            "SU" => (Math.Sqrt(Math.Pow(p["l"], 2) + Math.Pow(p["r"], 2)), 2 * (p["a"] + p["b"])),
            "TD" => (Math.Sqrt(Math.Pow(p["l"], 2) + Math.Pow(p["r"], 2)), 2 * (p["a"] + p["b"])),
            _ => throw new InvalidOperationException($"Nieznany typ kształtki: {type}")
        };
    }

    private static (double Length, double Perimeter) CalculateLinearOvalA(Dictionary<string, double> p)
    {
        double perimeter = p["A"] + p["B"] >= (2 * Math.PI * Math.Sqrt((2 * p["D"] + 2 * p["C"]) / 2)) / 2
            ? 2 * (p["A"] + p["B"])
            : 2 * Math.PI * Math.Sqrt((2 * p["D"] + 2 * p["C"]) / 2);
        double length = Math.Sqrt(Math.Pow(p["L"], 2) + Math.Pow(Math.Max(p["B"] - p["D"] + p["E"], Math.Max(p["A"] - p["D"] + p["F"], Math.Max(p["E"], p["F"]))), 2));
        return (length, perimeter);
    }

    private static (double Length, double Perimeter) CalculateMagicadOvalA(Dictionary<string, double> p)
    {
        double perimeter = p["a"] + p["b"] >= (2 * Math.PI * Math.Sqrt((2 * p["d"] + 2 * p["c"]) / 2)) / 2
            ? 2 * (p["a"] + p["b"])
            : 2 * Math.PI * Math.Sqrt((2 * p["d"] + 2 * p["c"]) / 2);
        double length = Math.Sqrt(Math.Pow(p["l"], 2) + Math.Pow(Math.Max(p["b"] - p["d"] + p["e"], Math.Max(p["a"] - p["d"] + p["f"], Math.Max(p["e"], p["f"]))), 2));
        return (length, perimeter);
    }

    private static (double Length, double Perimeter) CalculateLinearRoundS(Dictionary<string, double> p)
    {
        double perimeter = p["A"] + p["B"] >= (Math.PI * p["D"]) / 2 ? 2 * (p["A"] + p["B"]) : Math.PI * p["D"];
        double length = Math.Sqrt(Math.Pow(p["L"], 2) + Math.Pow(Math.Max(p["E"], p["F"]), 2));
        return (length, perimeter);
    }

    private static (double Length, double Perimeter) CalculateMagicadRoundS(Dictionary<string, double> p)
    {
        double perimeter = p["a"] + p["b"] >= (Math.PI * p["d"]) / 2 ? 2 * (p["a"] + p["b"]) : Math.PI * p["d"];
        double length = Math.Sqrt(Math.Pow(p["l"], 2) + Math.Pow(Math.Max(p["e"], p["f"]), 2));
        return (length, perimeter);
    }

    private static (double Length, double Perimeter) CalculateLinearRoundA(Dictionary<string, double> p)
    {
        double perimeter = p["A"] + p["B"] >= (Math.PI * p["D"]) / 2 ? 2 * (p["A"] + p["B"]) : Math.PI * p["D"];
        double length = Math.Sqrt(Math.Pow(p["L"], 2) + Math.Pow(Math.Max(p["B"] - p["D"] + p["E"], Math.Max(p["A"] - p["D"] + p["F"], Math.Max(p["E"], p["F"]))), 2));
        return (length, perimeter);
    }

    private static (double Length, double Perimeter) CalculateMagicadRoundA(Dictionary<string, double> p)
    {
        double perimeter = p["a"] + p["b"] >= (Math.PI * p["d"]) / 2 ? 2 * (p["a"] + p["b"]) : Math.PI * p["d"];
        double length = Math.Sqrt(Math.Pow(p["l"], 2) + Math.Pow(Math.Max(p["b"] - p["d"] + p["e"], Math.Max(p["a"] - p["d"] + p["f"], Math.Max(p["e"], p["f"]))), 2));
        return (length, perimeter);
    }

    private static (double Length, double Perimeter) CalculateLinearTg(Dictionary<string, double> p)
    {
        double perimeter1 = 2 * (p["A"] + Math.Max(p["B"], p["D"]));
        double length1 = p["L"];
        double perimeter2 = 2 * (p["A"] + p["H"]);
        double length2 = Math.Max(p["D"] + p["M"] - p["B"], p["M"]);
        return (length1 * perimeter1 + length2 * perimeter2, 1);
    }

    private static (double Length, double Perimeter) CalculateMagicadTg(Dictionary<string, double> p)
    {
        double perimeter1 = 2 * (p["a"] + Math.Max(p["b"], p["d"]));
        double length1 = p["l"];
        double perimeter2 = 2 * (p["a"] + p["h"]);
        double length2 = Math.Max(p["d"] + p["m"] - p["b"], p["m"]);
        return (length1 * perimeter1 + length2 * perimeter2, 1);
    }

    private static (double Length, double Perimeter) CalculateLinearTa(Dictionary<string, double> p)
    {
        double perimeter1 = 2 * (p["B"] >= p["D"] ? p["A"] + p["B"] : p["C"] + p["D"]);
        double length1 = Math.Sqrt(Math.Pow(p["L"], 2) + Math.Pow(p["E"], 2));
        double perimeter2 = 2 * (p["A"] + p["H"]);
        double length2 = Math.Max(p["D"] + p["M"] - p["B"] - p["E"], p["M"]);
        return (length1 * perimeter1 + length2 * perimeter2, 1);
    }

    private static (double Length, double Perimeter) CalculateMagicadTa(Dictionary<string, double> p)
    {
        double perimeter1 = 2 * (p["b"] >= p["d"] ? p["a"] + p["b"] : p["c"] + p["d"]);
        double length1 = Math.Sqrt(Math.Pow(p["l"], 2) + Math.Pow(p["e"], 2));
        double perimeter2 = 2 * (p["a"] + p["h"]);
        double length2 = Math.Max(p["d"] + p["m"] - p["b"] - p["e"], p["m"]);
        return (length1 * perimeter1 + length2 * perimeter2, 1);
    }

    private static (double Length, double Perimeter) CalculateLinearHs(Dictionary<string, double> p)
    {
        double m = Math.Max(p["M"], 100);
        double perimeter = p["B"] >= p["D"] + m + p["H"]
            ? 2 * (p["A"] + p["B"])
            : 2 * (p["C"] + p["D"] + m - p["H"]);
        double length = Math.Sqrt(Math.Pow(p["L"], 2) + Math.Pow(p["B"] >= p["D"] + m + p["H"] ? p["B"] - p["H"] - m - p["D"] + p["E"] : p["E"], 2));
        return (length, perimeter);
    }

    private static (double Length, double Perimeter) CalculateMagicadHs(Dictionary<string, double> p)
    {
        double m = Math.Max(p["m"], 100);
        double perimeter = p["b"] >= p["d"] + m + p["h"]
            ? 2 * (p["a"] + p["b"])
            : 2 * (p["c"] + p["d"] + m - p["h"]);
        double length = Math.Sqrt(Math.Pow(p["l"], 2) + Math.Pow(p["b"] >= p["d"] + m + p["h"] ? p["b"] - p["h"] - m - p["d"] + p["e"] : p["e"], 2));
        return (length, perimeter);
    }
}

public sealed record DuctFittingProfile(
    string DimensionPrefix,
    string DimensionTypeParameter,
    string AngleParameter,
    IReadOnlyList<string> Dimensions,
    IReadOnlyDictionary<string, string[]> RequiredParams,
    string ExtraDoubleParameter = "");
