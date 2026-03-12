using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.Estimate.Models;

namespace HCB.RevitAddin.Features.Estimate;

public sealed class EstimateService
{
    private const string PriceParameterName = "HC_Cena_Jednostkowa";
    private const string CostParameterName = "HC_Koszt";
    private const string AngleParameterName = "HC_Kat";
    private const string LegacyAngleParameterName = "HC_K�t";
    private const string AreaParameterName = "HC_Area";

    private static readonly BuiltInCategory[] SupportedCategories =
    [
        BuiltInCategory.OST_DuctCurves,
        BuiltInCategory.OST_DuctFitting,
        BuiltInCategory.OST_DuctAccessory,
        BuiltInCategory.OST_FlexDuctCurves
    ];

    private static readonly int[] RectUpperBounds = [500, 1000, 1500, 2000, 2500];

    public EstimateResult Apply(Document document, EstimateOptions options)
    {
        List<EstimateCatalogRow> catalogRows = LoadCatalog(options.CatalogPath);
        EstimateResult result = new();
        if (catalogRows.Count == 0)
        {
            result.Messages.Add("Cennik jest pusty albo nie udalo sie go odczytac.");
            return result;
        }

        double foilPrice = ResolveFoilPrice(catalogRows, options.AddFoil, result);
        List<Element> elements = CollectElements(document);

        using Transaction transaction = new(document, "Estimate");
        transaction.Start();

        foreach (Element element in elements)
        {
            result.ProcessedCount++;
            try
            {
                ProcessElement(element, catalogRows, options.AddFoil, foilPrice, result);
            }
            catch (Exception exception)
            {
                result.UnmatchedRows.Add(new EstimateUnmatchedRow(
                    element.Id.Value,
                    GetCategoryName(element) ?? string.Empty,
                    GetTypeName(element),
                    GetSizeString(element),
                    GetAngleValue(element),
                    $"ERR: {exception.Message}"));
            }
        }

        transaction.Commit();
        return result;
    }

    private static void ProcessElement(
        Element element,
        IReadOnlyList<EstimateCatalogRow> catalogRows,
        bool addFoil,
        double foilPrice,
        EstimateResult result)
    {
        Parameter? priceParameter = element.LookupParameter(PriceParameterName);
        Parameter? costParameter = element.LookupParameter(CostParameterName);
        if (priceParameter == null || costParameter == null || priceParameter.IsReadOnly || costParameter.IsReadOnly)
        {
            result.UnmatchedRows.Add(CreateUnmatchedRow(element, "Parametry kosztowe sa niedostepne."));
            return;
        }

        string? categoryName = GetCategoryName(element);
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            result.UnmatchedRows.Add(CreateUnmatchedRow(element, "Nieobslugiwana kategoria."));
            return;
        }

        string sizeString = GetSizeString(element);
        if (string.IsNullOrWhiteSpace(sizeString))
        {
            result.UnmatchedRows.Add(CreateUnmatchedRow(element, "Brak Size."));
            return;
        }

        string typeName = GetTypeName(element);
        int? angleValue = GetAngleValue(element);
        double? unitPrice = null;
        double? cost = null;
        double quantity = 0;
        string quantityUnit = string.Empty;
        string pricingBasis = string.Empty;
        bool isRectangular = sizeString.IndexOf('x', StringComparison.OrdinalIgnoreCase) >= 0 || sizeString.Contains("×");

        if (isRectangular)
        {
            if (categoryName is "Duct" or "Duct Fitting")
            {
                if (categoryName == "Duct Fitting" && string.Equals(GetFamilyName(element), "L_Flange_RV", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                int? maxRectDimension = GetMaxRectDimensionMillimeters(sizeString);
                if (!maxRectDimension.HasValue)
                {
                    result.UnmatchedRows.Add(CreateUnmatchedRow(element, "Nieczytelny rozmiar prostokatny."));
                    return;
                }

                EstimateCatalogRow? row = FindRectBucketPrice(catalogRows, categoryName, maxRectDimension.Value);
                if (row == null || !string.Equals(row.Unit, "m2", StringComparison.OrdinalIgnoreCase))
                {
                    result.UnmatchedRows.Add(CreateUnmatchedRow(element, "Brak ceny dla prostokatnego m2."));
                    return;
                }

                double? areaSquareMeters = GetAreaSquareMeters(element.LookupParameter(AreaParameterName));
                if (!areaSquareMeters.HasValue || areaSquareMeters.Value <= 0)
                {
                    result.UnmatchedRows.Add(CreateUnmatchedRow(element, "Brak HC_Area."));
                    return;
                }

                unitPrice = row.UnitPrice;
                cost = row.UnitPrice * areaSquareMeters.Value;
                if (addFoil && foilPrice > 0)
                {
                    cost += areaSquareMeters.Value * foilPrice;
                }

                quantity = areaSquareMeters.Value;
                quantityUnit = "m2";
                pricingBasis = "HC_Area";
                result.TotalAreaSquareMeters += areaSquareMeters.Value;
            }
            else if (categoryName == "Duct Accessory")
            {
                List<int> diameters = GetRoundDiameters(sizeString);
                EstimateCatalogRow? row = diameters.Count == 0
                    ? null
                    : FindRoundTypeNamePrice(catalogRows, categoryName, typeName, diameters, null);
                if (row == null || !string.Equals(row.Unit, "szt", StringComparison.OrdinalIgnoreCase))
                {
                    result.UnmatchedRows.Add(CreateUnmatchedRow(element, "Brak ceny akcesorium."));
                    return;
                }

                unitPrice = row.UnitPrice;
                cost = row.UnitPrice;
                quantity = 1;
                quantityUnit = "szt";
                pricingBasis = "Typ + rozmiar";
            }
        }
        else
        {
            List<int> diameters = GetRoundDiameters(sizeString);
            if (diameters.Count == 0)
            {
                result.UnmatchedRows.Add(CreateUnmatchedRow(element, "Brak srednic."));
                return;
            }

            switch (categoryName)
            {
                case "Duct":
                case "Flex Duct":
                {
                    EstimateCatalogRow? row = categoryName == "Flex Duct"
                        ? FindRoundAnySizePrice(catalogRows, diameters[0])
                        : FindRoundAnyPrice(catalogRows, categoryName, diameters[0]);
                    if (row == null || !string.Equals(row.Unit, "m", StringComparison.OrdinalIgnoreCase))
                    {
                        result.UnmatchedRows.Add(CreateUnmatchedRow(element, "Brak ceny dla kanalu okraglego."));
                        return;
                    }

                    double lengthMeters = GetLengthMeters(element);
                    unitPrice = row.UnitPrice;
                    cost = row.UnitPrice * lengthMeters;
                    quantity = lengthMeters;
                    quantityUnit = "m";
                    pricingBasis = "Dlugosc";
                    result.TotalLengthMeters += lengthMeters;
                    break;
                }

                case "Duct Fitting":
                {
                    if (!angleValue.HasValue)
                    {
                        result.MissingAngleElementIds.Add(element.Id.Value);
                    }

                    EstimateCatalogRow? row = FindRoundTypeNamePrice(catalogRows, categoryName, typeName, diameters, angleValue);
                    if (row == null || !string.Equals(row.Unit, "szt", StringComparison.OrdinalIgnoreCase))
                    {
                        result.UnmatchedRows.Add(CreateUnmatchedRow(element, "Brak ceny ksztaltki okraglej."));
                        return;
                    }

                    unitPrice = row.UnitPrice;
                    cost = row.UnitPrice;
                    quantity = 1;
                    quantityUnit = "szt";
                    pricingBasis = "Typ + rozmiar";
                    break;
                }

                case "Duct Accessory":
                {
                    EstimateCatalogRow? row = FindRoundTypeNamePrice(catalogRows, categoryName, typeName, diameters, null);
                    if (row == null || !string.Equals(row.Unit, "szt", StringComparison.OrdinalIgnoreCase))
                    {
                        result.UnmatchedRows.Add(CreateUnmatchedRow(element, "Brak ceny akcesorium okraglego."));
                        return;
                    }

                    unitPrice = row.UnitPrice;
                    cost = row.UnitPrice;
                    quantity = 1;
                    quantityUnit = "szt";
                    pricingBasis = "Typ + rozmiar";
                    break;
                }
            }
        }

        if (!unitPrice.HasValue || !cost.HasValue)
        {
            return;
        }

        priceParameter.Set(unitPrice.Value);
        costParameter.Set(cost.Value);
        result.UpdatedCount++;
        result.TotalCost += cost.Value;
        result.AppliedRows.Add(new EstimateAppliedRow(
            element.Id.Value,
            categoryName,
            typeName,
            sizeString,
            angleValue,
            pricingBasis,
            quantity,
            quantityUnit,
            unitPrice.Value,
            cost.Value));
    }

    private static List<Element> CollectElements(Document document)
    {
        List<Element> elements = [];
        foreach (BuiltInCategory category in SupportedCategories)
        {
            elements.AddRange(new FilteredElementCollector(document)
                .OfCategory(category)
                .WhereElementIsNotElementType()
                .ToElements());
        }

        return elements;
    }

    private static List<EstimateCatalogRow> LoadCatalog(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2)
        {
            return [];
        }

        char delimiter = DetectDelimiter(lines[0]);
        string[] headers = lines[0].Split(delimiter);
        Dictionary<string, int> headerMap = headers
            .Select((value, index) => new { value, index })
            .GroupBy(item => item.value.Trim().ToLowerInvariant())
            .ToDictionary(group => group.Key, group => group.First().index);

        List<EstimateCatalogRow> rows = [];
        foreach (string line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] values = line.Split(delimiter);
            string category = GetField(values, headerMap, "category");
            string shape = GetField(values, headerMap, "shape").ToLowerInvariant();
            string matchKey = NormalizeMatchKey(GetField(values, headerMap, "matchkey"));
            string sizePattern = GetField(values, headerMap, "sizepattern");
            string angleRaw = GetField(values, headerMap, "angle");
            string unit = GetField(values, headerMap, "unit").ToLowerInvariant().Replace("²", "2");
            string unitPriceRaw = GetField(values, headerMap, "unitprice").Replace(" ", string.Empty);
            string typeNamePattern = GetField(values, headerMap, "typenamepattern").ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(category)
                || string.IsNullOrWhiteSpace(shape)
                || string.IsNullOrWhiteSpace(unit)
                || !TryParseDouble(unitPriceRaw, out double unitPrice))
            {
                continue;
            }

            ParseSizePattern(shape, sizePattern, matchKey, out double? low, out double? high, out string normalizedMatchKey);
            rows.Add(new EstimateCatalogRow(
                category.Trim(),
                shape.Trim(),
                normalizedMatchKey,
                low,
                high,
                TryParseInt(angleRaw),
                unit.Trim(),
                unitPrice,
                typeNamePattern.Trim()));
        }

        return rows;
    }

    private static double ResolveFoilPrice(IReadOnlyList<EstimateCatalogRow> catalogRows, bool addFoil, EstimateResult result)
    {
        if (!addFoil)
        {
            return 0;
        }

        EstimateCatalogRow? row = catalogRows.FirstOrDefault(item =>
            string.Equals(item.Category, "Foliowanie", StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.Shape, "rect", StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.MatchKey, "ANY", StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.Unit, "m2", StringComparison.OrdinalIgnoreCase));

        if (row == null)
        {
            result.Messages.Add("Brak pozycji foliowania w cenniku. Foliowanie zostalo pominiete.");
            return 0;
        }

        return row.UnitPrice;
    }

    private static EstimateCatalogRow? FindRoundAnyPrice(IReadOnlyList<EstimateCatalogRow> rows, string category, int diameterMillimeters)
    {
        return rows.FirstOrDefault(row =>
            string.Equals(row.Category, category, StringComparison.OrdinalIgnoreCase)
            && string.Equals(row.Shape, "round", StringComparison.OrdinalIgnoreCase)
            && string.Equals(row.MatchKey, "ANY", StringComparison.OrdinalIgnoreCase)
            && row.SizePatternLow.HasValue
            && Math.Round(row.SizePatternLow.Value) == diameterMillimeters);
    }

    private static EstimateCatalogRow? FindRoundAnySizePrice(IReadOnlyList<EstimateCatalogRow> rows, int diameterMillimeters)
    {
        return rows.FirstOrDefault(row =>
            string.Equals(row.Shape, "round", StringComparison.OrdinalIgnoreCase)
            && string.Equals(row.MatchKey, "ANY", StringComparison.OrdinalIgnoreCase)
            && row.SizePatternLow.HasValue
            && Math.Round(row.SizePatternLow.Value) == diameterMillimeters);
    }

    private static EstimateCatalogRow? FindRoundTypeNamePrice(
        IReadOnlyList<EstimateCatalogRow> rows,
        string category,
        string typeName,
        IReadOnlyList<int> diameterCandidatesMillimeters,
        int? angle)
    {
        List<int> uniqueDiameters = diameterCandidatesMillimeters.Distinct().OrderByDescending(value => value).ToList();
        List<string> typeVariants = [typeName.ToLowerInvariant()];
        if (typeVariants[0].Contains("bku", StringComparison.Ordinal))
        {
            typeVariants.Add(typeVariants[0].Replace("bku", "bu", StringComparison.Ordinal));
        }

        foreach (string typeVariant in typeVariants)
        {
            if (uniqueDiameters.Count >= 2)
            {
                int largerDiameter = uniqueDiameters[0];
                int smallerDiameter = uniqueDiameters[1];
                EstimateCatalogRow? pairRow = rows.FirstOrDefault(row =>
                    string.Equals(row.Category, category, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(row.Shape, "round", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(row.MatchKey, "PAIR", StringComparison.OrdinalIgnoreCase)
                    && row.TypeNamePattern.Length > 0
                    && typeVariant.Contains(row.TypeNamePattern, StringComparison.OrdinalIgnoreCase)
                    && row.SizePatternLow.HasValue
                    && row.SizePatternHigh.HasValue
                    && Math.Round(Math.Max(row.SizePatternLow.Value, row.SizePatternHigh.Value)) == largerDiameter
                    && Math.Round(Math.Min(row.SizePatternLow.Value, row.SizePatternHigh.Value)) == smallerDiameter
                    && (!angle.HasValue || row.Angle == angle || row.Angle == null));
                if (pairRow != null)
                {
                    return pairRow;
                }
            }

            foreach (int? angleCheck in angle.HasValue ? new int?[] { angle, null } : [null])
            {
                foreach (int diameter in uniqueDiameters)
                {
                    EstimateCatalogRow? row = rows.FirstOrDefault(candidate =>
                        string.Equals(candidate.Category, category, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(candidate.Shape, "round", StringComparison.OrdinalIgnoreCase)
                        && (string.Equals(candidate.MatchKey, "TYPENAME", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(candidate.MatchKey, "PAIR", StringComparison.OrdinalIgnoreCase))
                        && candidate.TypeNamePattern.Length > 0
                        && typeVariant.Contains(candidate.TypeNamePattern, StringComparison.OrdinalIgnoreCase)
                        && candidate.SizePatternLow.HasValue
                        && Math.Round(candidate.SizePatternLow.Value) == diameter
                        && candidate.Angle == angleCheck);
                    if (row != null)
                    {
                        return row;
                    }
                }
            }
        }

        return null;
    }

    private static EstimateCatalogRow? FindRectBucketPrice(IReadOnlyList<EstimateCatalogRow> rows, string category, int maxDimensionMillimeters)
    {
        int upperBound = RectUpperBounds.FirstOrDefault(bound => maxDimensionMillimeters <= bound);
        if (upperBound == 0)
        {
            return null;
        }

        return rows.FirstOrDefault(row =>
            string.Equals(row.Category, category, StringComparison.OrdinalIgnoreCase)
            && string.Equals(row.Shape, "rect", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(row.MatchKey, "MAX_DIM_LEQ", StringComparison.OrdinalIgnoreCase)
                || string.Equals(row.MatchKey, "MAX_DIM", StringComparison.OrdinalIgnoreCase))
            && row.RectUpperBoundMillimeters == upperBound);
    }

    private static string? GetCategoryName(Element element)
    {
        BuiltInCategory? category = element.Category?.BuiltInCategory;
        return category switch
        {
            BuiltInCategory.OST_DuctCurves => "Duct",
            BuiltInCategory.OST_DuctFitting => "Duct Fitting",
            BuiltInCategory.OST_DuctAccessory => "Duct Accessory",
            BuiltInCategory.OST_FlexDuctCurves => "Flex Duct",
            _ => null
        };
    }

    private static string GetSizeString(Element element)
    {
        Parameter? sizeParameter = element.get_Parameter(BuiltInParameter.RBS_CALCULATED_SIZE)
            ?? element.LookupParameter("Size");
        return sizeParameter?.AsString()?.Trim()
            ?? sizeParameter?.AsValueString()?.Trim()
            ?? string.Empty;
    }

    private static string GetTypeName(Element element)
    {
        try
        {
            Parameter? typeParameter = element.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM);
            if (typeParameter?.StorageType == StorageType.ElementId)
            {
                Element? typeElement = element.Document.GetElement(typeParameter.AsElementId());
                string? typeName = typeElement?.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME)?.AsString();
                if (!string.IsNullOrWhiteSpace(typeName))
                {
                    return typeName.Trim();
                }

                if (!string.IsNullOrWhiteSpace(typeElement?.Name))
                {
                    return typeElement.Name;
                }
            }
        }
        catch
        {
        }

        if (element is FamilyInstance familyInstance && familyInstance.Symbol != null && !string.IsNullOrWhiteSpace(familyInstance.Symbol.Name))
        {
            return familyInstance.Symbol.Name;
        }

        return element.Name ?? string.Empty;
    }

    private static string GetFamilyName(Element element)
    {
        return element is FamilyInstance familyInstance && familyInstance.Symbol != null
            ? familyInstance.Symbol.FamilyName ?? string.Empty
            : string.Empty;
    }

    private static double GetLengthMeters(Element element)
    {
        Parameter? parameter = element.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
        return parameter?.StorageType == StorageType.Double
            ? UnitUtils.ConvertFromInternalUnits(parameter.AsDouble(), UnitTypeId.Meters)
            : 0;
    }

    private static int? GetAngleValue(Element element)
    {
        Parameter? parameter = element.LookupParameter(AngleParameterName)
            ?? element.LookupParameter(LegacyAngleParameterName);
        if (parameter == null)
        {
            return null;
        }

        try
        {
            return parameter.StorageType switch
            {
                StorageType.Double => (int)Math.Round(UnitUtils.ConvertFromInternalUnits(parameter.AsDouble(), UnitTypeId.Degrees)),
                StorageType.Integer => parameter.AsInteger(),
                StorageType.String when TryParseDouble(parameter.AsString(), out double value) => (int)Math.Round(value),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static double? GetAreaSquareMeters(Parameter? parameter)
    {
        if (parameter == null || parameter.StorageType != StorageType.Double)
        {
            return null;
        }

        ForgeTypeId? specTypeId = parameter.Definition?.GetDataType();
        return specTypeId != null && specTypeId == SpecTypeId.Area
            ? UnitUtils.ConvertFromInternalUnits(parameter.AsDouble(), UnitTypeId.SquareMeters)
            : parameter.AsDouble();
    }

    private static EstimateUnmatchedRow CreateUnmatchedRow(Element element, string reason)
    {
        return new EstimateUnmatchedRow(
            element.Id.Value,
            GetCategoryName(element) ?? string.Empty,
            GetTypeName(element),
            GetSizeString(element),
            GetAngleValue(element),
            reason);
    }

    private static List<int> GetRoundDiameters(string sizeString)
    {
        if (string.IsNullOrWhiteSpace(sizeString))
        {
            return [];
        }

        string normalized = sizeString
            .Trim()
            .ToLowerInvariant()
            .Replace("⌀", string.Empty)
            .Replace("ø", string.Empty)
            .Replace(" ", string.Empty)
            .Replace("×", "x");

        string[] tokens = Regex.Split(normalized, "[-/_x]");
        List<int> values = [];
        foreach (string token in tokens)
        {
            double? millimeters = ToMillimeters(token);
            if (millimeters.HasValue)
            {
                values.Add((int)Math.Round(millimeters.Value));
            }
        }

        return values;
    }

    private static int? GetMaxRectDimensionMillimeters(string sizeString)
    {
        if (string.IsNullOrWhiteSpace(sizeString))
        {
            return null;
        }

        string normalized = sizeString.ToLowerInvariant().Replace(" ", string.Empty).Replace("×", "x");
        int? maxDimension = null;
        foreach (string part in normalized.Split('-'))
        {
            Match match = Regex.Match(part, @"^(\d+)x(\d+)$");
            if (!match.Success)
            {
                continue;
            }

            int width = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            int height = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            int candidate = Math.Max(width, height);
            maxDimension = !maxDimension.HasValue || candidate > maxDimension.Value ? candidate : maxDimension;
        }

        if (maxDimension.HasValue)
        {
            return maxDimension.Value;
        }

        MatchCollection numericMatches = Regex.Matches(normalized, @"\d+");
        return numericMatches.Count > 0
            ? numericMatches.Cast<Match>().Select(match => int.Parse(match.Value, CultureInfo.InvariantCulture)).Max()
            : null;
    }

    private static double? ToMillimeters(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        Match match = Regex.Match(token, @"^([0-9]*[.,]?[0-9]+)(mm|cm|m)?$");
        if (!match.Success || !TryParseDouble(match.Groups[1].Value, out double value))
        {
            return null;
        }

        string unit = match.Groups[2].Success ? match.Groups[2].Value : "mm";
        return unit switch
        {
            "mm" => value,
            "cm" => value * 10.0,
            "m" => value * 1000.0,
            _ => null
        };
    }

    private static void ParseSizePattern(string shape, string sizePattern, string matchKey, out double? low, out double? high, out string normalizedMatchKey)
    {
        low = null;
        high = null;
        normalizedMatchKey = matchKey;
        string normalizedPattern = (sizePattern ?? string.Empty).Replace(" ", string.Empty);

        if (string.Equals(shape, "round", StringComparison.OrdinalIgnoreCase)
            && (normalizedPattern.Contains('-') || normalizedPattern.Contains('/'))
            && !normalizedPattern.Contains('x', StringComparison.OrdinalIgnoreCase)
            && !normalizedPattern.Contains("×", StringComparison.OrdinalIgnoreCase))
        {
            char separator = normalizedPattern.Contains('-') ? '-' : '/';
            string[] parts = normalizedPattern.Split(separator);
            if (parts.Length == 2
                && TryParseDouble(parts[0], out double first)
                && TryParseDouble(parts[1], out double second))
            {
                low = Math.Min(first, second);
                high = Math.Max(first, second);
                if (string.IsNullOrWhiteSpace(normalizedMatchKey))
                {
                    normalizedMatchKey = "PAIR";
                }
            }

            return;
        }

        if (normalizedPattern.Contains('-'))
        {
            string[] parts = normalizedPattern.Split('-', 2);
            if (TryParseDouble(parts[0], out double first) && TryParseDouble(parts[1], out double second))
            {
                low = first;
                high = second;
            }

            return;
        }

        if (TryParseDouble(normalizedPattern, out double value))
        {
            low = value;
            high = value;
        }
    }

    private static string NormalizeMatchKey(string value)
    {
        string normalized = Regex.Replace(value ?? string.Empty, @"\s+", string.Empty).ToUpperInvariant();
        return normalized switch
        {
            "MAXDIMLEQ" or "MAX_DIM_LEQ" or "MAX-DIM-LEQ" => "MAX_DIM_LEQ",
            "PAIR" or "REDUCTION" or "REDUKCJA" => "PAIR",
            _ => normalized
        };
    }

    private static char DetectDelimiter(string line)
    {
        int semicolons = line.Count(character => character == ';');
        int commas = line.Count(character => character == ',');
        if (semicolons >= commas && semicolons > 0)
        {
            return ';';
        }

        return line.Contains('\t') ? '\t' : ',';
    }

    private static string GetField(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> headerMap, string fieldName)
    {
        return headerMap.TryGetValue(fieldName.ToLowerInvariant(), out int index) && index < values.Count
            ? values[index].Trim()
            : string.Empty;
    }

    private static bool TryParseDouble(string? rawValue, out double value)
    {
        return double.TryParse(
            (rawValue ?? string.Empty).Trim().Replace(",", "."),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static int? TryParseInt(string? rawValue)
    {
        return TryParseDouble(rawValue, out double value)
            ? (int?)Math.Round(value)
            : null;
    }
}

