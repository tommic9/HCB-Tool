using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.ColorUniqueSystems.Models;

namespace HCB.RevitAddin.Features.ColorUniqueSystems;

public sealed class ColorUniqueSystemsService
{
    private readonly IReadOnlyList<SystemColorOption> _allSystems;
    private readonly string _configurationSource;
    private readonly bool _usesFallbackPreset;

    public ColorUniqueSystemsService()
    {
        (_allSystems, _configurationSource, _usesFallbackPreset) = LoadCatalog();
    }

    public IReadOnlyList<SystemColorOption> GetAvailableSystems() => _allSystems;

    public string GetConfigurationSourceLabel() => _configurationSource;

    public bool UsesFallbackPreset() => _usesFallbackPreset;

    public ColorUniqueSystemsResult Apply(Document document, View view, IEnumerable<SystemColorOption> selectedOptions, bool overrideDisplayLines)
    {
        List<SystemColorOption> options = selectedOptions.ToList();
        ColorUniqueSystemsResult result = new()
        {
            ConfigurationSource = _configurationSource,
            UsedFallbackPreset = _usesFallbackPreset
        };

        if (options.Count == 0)
        {
            result.Messages.Add("Nie wybrano zadnych systemow.");
            return result;
        }

        ElementId solidFillPatternId = new FilteredElementCollector(document)
            .OfClass(typeof(FillPatternElement))
            .Cast<FillPatternElement>()
            .First(fill => fill.GetFillPattern().IsSolidFill)
            .Id;

        using Transaction transaction = new(document, "Color Unique Systems");
        transaction.Start();

        foreach (SystemColorOption option in options)
        {
            ParameterFilterElement filter = FindOrCreateFilter(document, option);
            if (!view.GetFilters().Contains(filter.Id))
            {
                view.AddFilter(filter.Id);
            }

            OverrideGraphicSettings overrides = new();
            overrides.SetSurfaceForegroundPatternId(solidFillPatternId);
            overrides.SetSurfaceForegroundPatternColor(new Color(option.Red, option.Green, option.Blue));
            if (overrideDisplayLines)
            {
                overrides.SetProjectionLineColor(new Color(0, 0, 0));
                overrides.SetProjectionLineWeight(1);
            }

            view.SetFilterOverrides(filter.Id, overrides);

            result.AppliedCount++;
            result.Messages.Add($"Zastosowano: {option.SystemName}");
        }

        transaction.Commit();
        return result;
    }

    private static ParameterFilterElement FindOrCreateFilter(Document document, SystemColorOption option)
    {
        ParameterFilterElement? existingFilter = new FilteredElementCollector(document)
            .OfClass(typeof(ParameterFilterElement))
            .Cast<ParameterFilterElement>()
            .FirstOrDefault(filter => filter.Name == option.SystemName);

        ElementId parameterId = option.IsVentilation
            ? new(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM)
            : new(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);

        FilterRule rule = ParameterFilterRuleFactory.CreateContainsRule(parameterId, option.EffectiveFilterValue);
        ElementFilter elementFilter = new ElementParameterFilter(rule);

        if (existingFilter != null)
        {
            existingFilter.SetElementFilter(elementFilter);
            return existingFilter;
        }

        List<ElementId> categories = option.IsVentilation
            ? [new(BuiltInCategory.OST_DuctCurves), new(BuiltInCategory.OST_DuctFitting), new(BuiltInCategory.OST_DuctAccessory), new(BuiltInCategory.OST_DuctTerminal), new(BuiltInCategory.OST_FlexDuctCurves)]
            : [new(BuiltInCategory.OST_PipeCurves), new(BuiltInCategory.OST_PipeFitting), new(BuiltInCategory.OST_PipeAccessory), new(BuiltInCategory.OST_PlumbingFixtures)];

        return ParameterFilterElement.Create(document, option.SystemName, categories, elementFilter);
    }

    private static (IReadOnlyList<SystemColorOption> Systems, string Source, bool UsesFallback) LoadCatalog()
    {
        foreach (string path in GetCandidateCsvPaths())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                IReadOnlyList<SystemColorOption> parsed = ParseCsv(path);
                if (parsed.Count > 0)
                {
                    return (parsed, path, false);
                }
            }
            catch
            {
                // Fallback to preset below.
            }
        }

        return (CreateFallbackSystems(), "Preset w kodzie", true);
    }

    private static IReadOnlyList<string> GetCandidateCsvPaths()
    {
        string baseDirectory = AppContext.BaseDirectory;
        return
        [
            Path.Combine(baseDirectory, "Resources", "Config", "SystemColors.csv"),
            Path.Combine(baseDirectory, "kodyfikacja", "Systemy HCB.csv"),
            Path.Combine(Environment.CurrentDirectory, "kodyfikacja", "Systemy HCB.csv")
        ];
    }

    private static IReadOnlyList<SystemColorOption> ParseCsv(string path)
    {
        string[] lines = File.ReadAllLines(path);
        if (lines.Length <= 1)
        {
            return [];
        }

        string[] headers = SplitCsvLine(lines[0]);
        Dictionary<string, int> headerMap = headers
            .Select((header, index) => new { Header = header.Trim(), Index = index })
            .ToDictionary(item => item.Header, item => item.Index, StringComparer.OrdinalIgnoreCase);

        bool isExtendedFormat = headerMap.ContainsKey("SystemName") &&
                                headerMap.ContainsKey("GroupName") &&
                                headerMap.ContainsKey("FilterValue") &&
                                headerMap.ContainsKey("R") &&
                                headerMap.ContainsKey("G") &&
                                headerMap.ContainsKey("B");

        bool isLegacyFormat = headerMap.ContainsKey("Nazwa systemu") &&
                              headerMap.ContainsKey("Description (PL)") &&
                              headerMap.ContainsKey("Kolor (RGB)");

        List<SystemColorOption> systems = new();
        foreach (string rawLine in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            string[] values = SplitCsvLine(rawLine);
            SystemColorOption? option = isExtendedFormat
                ? ParseExtendedRow(values, headerMap)
                : isLegacyFormat
                    ? ParseLegacyRow(values, headerMap)
                    : null;

            if (option != null)
            {
                systems.Add(option);
            }
        }

        return systems;
    }

    private static SystemColorOption? ParseExtendedRow(string[] values, IReadOnlyDictionary<string, int> headerMap)
    {
        string systemName = GetValue(values, headerMap, "SystemName");
        if (string.IsNullOrWhiteSpace(systemName))
        {
            return null;
        }

        string discipline = GetValue(values, headerMap, "Discipline");
        bool isVentilation = discipline.Equals("Ventilation", StringComparison.OrdinalIgnoreCase);

        return new SystemColorOption(
            systemName,
            ParseByte(GetValue(values, headerMap, "R")),
            ParseByte(GetValue(values, headerMap, "G")),
            ParseByte(GetValue(values, headerMap, "B")),
            isVentilation,
            GetValue(values, headerMap, "DisplayName"),
            GetValue(values, headerMap, "GroupName"),
            GetValue(values, headerMap, "FilterValue"));
    }

    private static SystemColorOption? ParseLegacyRow(string[] values, IReadOnlyDictionary<string, int> headerMap)
    {
        string systemName = GetValue(values, headerMap, "Nazwa systemu");
        if (string.IsNullOrWhiteSpace(systemName))
        {
            return null;
        }

        (byte red, byte green, byte blue) = ParseLegacyRgb(GetValue(values, headerMap, "Kolor (RGB)"));
        bool isVentilation = systemName.StartsWith("V_", StringComparison.OrdinalIgnoreCase);

        return new SystemColorOption(
            systemName,
            red,
            green,
            blue,
            isVentilation,
            GetValue(values, headerMap, "Description (PL)"),
            GetLegacyGroupName(systemName),
            GetLegacyFilterValue(systemName, isVentilation));
    }

    private static string[] SplitCsvLine(string line)
    {
        return line.Split(';');
    }

    private static string GetValue(string[] values, IReadOnlyDictionary<string, int> headerMap, string header)
    {
        return headerMap.TryGetValue(header, out int index) && index >= 0 && index < values.Length
            ? values[index].Trim()
            : string.Empty;
    }

    private static byte ParseByte(string value)
    {
        return byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte parsed)
            ? parsed
            : (byte)0;
    }

    private static (byte Red, byte Green, byte Blue) ParseLegacyRgb(string value)
    {
        string normalized = value.Replace("(", string.Empty).Replace(")", string.Empty).Replace(" ", string.Empty);
        string[] parts = normalized.Split(',');
        if (parts.Length != 3)
        {
            return (0, 0, 0);
        }

        return (ParseByte(parts[0]), ParseByte(parts[1]), ParseByte(parts[2]));
    }

    private static string GetLegacyFilterValue(string systemName, bool isVentilation)
    {
        return isVentilation
            ? systemName.Split('(')[0].Trim()
            : systemName;
    }

    private static string GetLegacyGroupName(string systemName)
    {
        if (systemName.StartsWith("V_", StringComparison.OrdinalIgnoreCase))
        {
            return "Wentylacja";
        }

        if (systemName.StartsWith("CHW_", StringComparison.OrdinalIgnoreCase))
        {
            return "Chlodzenie";
        }

        if (systemName.StartsWith("REF_", StringComparison.OrdinalIgnoreCase))
        {
            return "Chlodnictwo";
        }

        if (systemName.StartsWith("VRV_", StringComparison.OrdinalIgnoreCase) ||
            systemName.StartsWith("SPL_", StringComparison.OrdinalIgnoreCase))
        {
            return "Freon / DX";
        }

        if (systemName.StartsWith("HP_", StringComparison.OrdinalIgnoreCase) ||
            systemName.StartsWith("HW_", StringComparison.OrdinalIgnoreCase) ||
            systemName.StartsWith("HWX_", StringComparison.OrdinalIgnoreCase) ||
            systemName.StartsWith("PH_", StringComparison.OrdinalIgnoreCase))
        {
            return "Grzanie";
        }

        if (systemName.StartsWith("WU_", StringComparison.OrdinalIgnoreCase))
        {
            return "Woda uzytkowa";
        }

        if (systemName.StartsWith("FP_", StringComparison.OrdinalIgnoreCase))
        {
            return "PPOZ";
        }

        if (systemName.StartsWith("SW", StringComparison.OrdinalIgnoreCase))
        {
            return "Kanalizacja";
        }

        if (systemName.StartsWith("RW", StringComparison.OrdinalIgnoreCase))
        {
            return "Deszczowka";
        }

        if (systemName.StartsWith("PA_", StringComparison.OrdinalIgnoreCase))
        {
            return "Sprezone powietrze";
        }

        return "Inne";
    }

    private static IReadOnlyList<SystemColorOption> CreateFallbackSystems()
    {
        return
        [
            new("V_Supply air (Nawiew)", 0, 128, 255, true, "Wentylacja - Nawiew", "Wentylacja", "V_Supply air"),
            new("V_Outdoor air (Czerpny)", 64, 192, 0, true, "Wentylacja - Czerpny", "Wentylacja", "V_Outdoor air"),
            new("V_Extract air (Wywiew)", 255, 255, 0, true, "Wentylacja - Wywiew", "Wentylacja", "V_Extract air"),
            new("V_Exhaust air (Wyrzut)", 192, 128, 0, true, "Wentylacja - Wyrzut", "Wentylacja", "V_Exhaust air"),
            new("CHW_Supply", 0, 92, 255, false, "Chlodzenie - Zasilanie", "Chlodzenie", "CHW_Supply"),
            new("CHW_Return", 0, 255, 255, false, "Chlodzenie - Powrot", "Chlodzenie", "CHW_Return"),
            new("HP_Supply", 128, 64, 128, false, "Grzanie/Chlodzenie - Zasilanie", "Grzanie", "HP_Supply"),
            new("HP_Return", 255, 64, 128, false, "Grzanie/Chlodzenie - Powrot", "Grzanie", "HP_Return"),
            new("HW_Supply", 255, 0, 0, false, "Grzanie - Zasilanie", "Grzanie", "HW_Supply"),
            new("HW_Return", 0, 0, 128, false, "Grzanie - Powrot", "Grzanie", "HW_Return"),
            new("HWX_Supply", 192, 0, 0, false, "Wymiennik - Zasilanie", "Grzanie", "HWX_Supply"),
            new("HWX_Return", 0, 64, 192, false, "Wymiennik - Powrot", "Grzanie", "HWX_Return"),
            new("WU_Circulation", 128, 0, 255, false, "Woda uzytkowa - Cyrkulacja", "Woda uzytkowa", "WU_Circulation"),
            new("WU_Cold Water", 0, 255, 0, false, "Woda uzytkowa - Zimna woda", "Woda uzytkowa", "WU_Cold Water"),
            new("WU_Hot Water", 255, 64, 0, false, "Woda uzytkowa - Ciepla woda", "Woda uzytkowa", "WU_Hot Water"),
            new("FP_Fire Protection", 255, 164, 8, false, "Instalacja PPOZ", "PPOZ", "FP_Fire Protection"),
            new("PH_Supply", 240, 0, 150, false, "Podlogowka - Zasilanie", "Grzanie", "PH_Supply"),
            new("PH_Return", 180, 0, 192, false, "Podlogowka - Powrot", "Grzanie", "PH_Return"),
            new("SW_Sewage Water", 128, 92, 64, false, "Kanalizacja", "Kanalizacja", "SW_Sewage Water"),
            new("SWC_Sewage Water Condensat", 128, 128, 92, false, "Kanalizacja - Skropliny", "Kanalizacja", "SWC_Sewage Water Condensat"),
            new("SWV_Sewage Water Ventilation", 164, 128, 64, false, "Kanalizacja - Odpowietrzenie", "Kanalizacja", "SWV_Sewage Water Ventilation"),
            new("RWP_Rainwater Pressure", 0, 128, 164, false, "Kanalizacja deszczowa - Cisnieniowa", "Deszczowka", "RWP_Rainwater Pressure"),
            new("RWG_Rainwater Gravity", 128, 164, 255, false, "Kanalizacja deszczowa - Grawitacyjna", "Deszczowka", "RWG_Rainwater Gravity"),
            new("RWE_Rainwater Emergency", 192, 128, 192, false, "Kanalizacja deszczowa - Awaryjna", "Deszczowka", "RWE_Rainwater Emergency"),
            new("VRV_Supply", 0, 192, 192, false, "VRV - Zasilanie", "Freon / DX", "VRV_Supply"),
            new("VRV_Return", 0, 92, 192, false, "VRV - Powrot", "Freon / DX", "VRV_Return"),
            new("REF_Supply", 24, 128, 200, false, "Chlodnictwo - Zasilanie", "Chlodnictwo", "REF_Supply"),
            new("REF_Return", 24, 192, 222, false, "Chlodnictwo - Powrot", "Chlodnictwo", "REF_Return"),
            new("SPL_Supply", 255, 164, 164, false, "Split - Zasilanie", "Freon / DX", "SPL_Supply"),
            new("SPL_Return", 255, 128, 164, false, "Split - Powrot", "Freon / DX", "SPL_Return"),
            new("PA_Pressure Air", 255, 102, 204, false, "Sprezone powietrze", "Sprezone powietrze", "PA_Pressure Air")
        ];
    }
}
