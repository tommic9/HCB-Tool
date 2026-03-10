using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.ColorVentSystems.Models;
using HCB.RevitAddin.Features.ColorUniqueSystems.Models;

namespace HCB.RevitAddin.Features.ColorVentSystems;

public sealed class ColorVentSystemsService
{
    private static readonly IReadOnlyList<SystemColorOption> VentilationSystems =
    [
        new("V_Supply air (Nawiew)", 0, 128, 255, true),
        new("V_Outdoor air (Czerpny)", 64, 192, 0, true),
        new("V_Extract air (Wywiew)", 255, 255, 0, true),
        new("V_Exhaust air (Wyrzut)", 192, 128, 0, true)
    ];

    private static readonly IReadOnlyList<SystemColorOption> PipingSystems =
    [
        new("CHW_Supply", 0, 92, 255, false),
        new("CHW_Return", 0, 255, 255, false),
        new("HP_Supply", 128, 64, 128, false),
        new("HP_Return", 255, 64, 128, false),
        new("HW_Supply", 255, 0, 0, false),
        new("HW_Return", 0, 0, 128, false),
        new("HWX_Supply", 192, 0, 0, false),
        new("HWX_Return", 0, 64, 192, false),
        new("WU_Circulation", 128, 0, 255, false),
        new("WU_Cold Water", 0, 255, 0, false),
        new("WU_Hot Water", 255, 64, 0, false),
        new("FP_Fire Protection", 255, 164, 8, false),
        new("PH_Supply", 240, 0, 150, false),
        new("PH_Return", 180, 0, 192, false),
        new("SW_Sewage Water", 128, 92, 64, false),
        new("SWC_Sewage Water Condensat", 128, 128, 92, false),
        new("SWV_Sewage Water Ventilation", 164, 128, 64, false),
        new("RWP_Rainwater Pressure", 0, 128, 164, false),
        new("RWG_Rainwater Gravity", 128, 164, 255, false),
        new("RWE_Rainwater Emergency", 192, 128, 192, false),
        new("VRV_Return", 0, 92, 192, false),
        new("VRV_Supply", 0, 192, 192, false),
        new("REF_Supply", 24, 128, 200, false),
        new("REF_Return", 24, 192, 222, false),
        new("SPL_Supply", 255, 164, 164, false),
        new("SPL_Return", 255, 128, 164, false)
    ];

    private static readonly IReadOnlyList<SystemColorPackageOption> Packages =
    [
        new("WENTYLACJA ALL", VentilationSystems.Select(item => item.SystemName).ToList()),
        new("Chlodzenie (CHW+VRV+REF+HP)", ["REF_Supply", "REF_Return", "VRV_Supply", "VRV_Return", "CHW_Supply", "CHW_Return", "HP_Supply", "HP_Return"]),
        new("Grzanie (HW+HWX+HP)", ["HW_Supply", "HW_Return", "HWX_Supply", "HWX_Return", "HP_Supply", "HP_Return"]),
        new("Podlogowka (PH)", ["PH_Supply", "PH_Return"]),
        new("Grzanie/chlodzenie (CHW+VRV+REF+HP+HW+HWX)", ["HW_Supply", "HW_Return", "HWX_Supply", "HWX_Return", "HP_Supply", "HP_Return", "CHW_Supply", "CHW_Return", "REF_Supply", "REF_Return", "VRV_Supply", "VRV_Return"]),
        new("Kanaliza (SW)", ["SW_Sewage Water", "SWC_Sewage Water Condensat", "SWV_Sewage Water Ventilation"]),
        new("Woda uzytkowa (WU)", ["WU_Circulation", "WU_Cold Water", "WU_Hot Water"]),
        new("Deszczowka (RW)", ["RWP_Rainwater Pressure", "RWG_Rainwater Gravity", "RWE_Rainwater Emergency"]),
        new("Ppoz (FP)", ["FP_Fire Protection"]),
        new("Sanitarne (WU+SW)", ["WU_Circulation", "WU_Cold Water", "WU_Hot Water", "SW_Sewage Water", "SWC_Sewage Water Condensat", "SWV_Sewage Water Ventilation"]),
        new("RUROWE ALL", PipingSystems.Select(item => item.SystemName).ToList()),
        new("WSZYSTKIE ALL", VentilationSystems.Select(item => item.SystemName).Concat(PipingSystems.Select(item => item.SystemName)).ToList())
    ];

    public IReadOnlyList<SystemColorPackageOption> GetAvailablePackages() => Packages;

    public ColorVentSystemsResult Apply(Document document, View view, IEnumerable<string> selectedPackageNames, bool overrideDisplayLines)
    {
        List<SystemColorOption> allSystems = VentilationSystems.Concat(PipingSystems).ToList();
        HashSet<string> selectedSystemNames = Packages
            .Where(option => selectedPackageNames.Contains(option.Name))
            .SelectMany(option => option.SystemNames)
            .ToHashSet();

        List<SystemColorOption> selectedOptions = allSystems
            .Where(option => selectedSystemNames.Contains(option.SystemName))
            .ToList();

        ColorVentSystemsResult result = new();
        if (selectedOptions.Count == 0)
        {
            result.Messages.Add("Nie wybrano zadnych pakietow do pokolorowania.");
            return result;
        }

        ElementId solidFillPatternId = new FilteredElementCollector(document)
            .OfClass(typeof(FillPatternElement))
            .Cast<FillPatternElement>()
            .First(fill => fill.GetFillPattern().IsSolidFill)
            .Id;

        using Transaction transaction = new(document, "Kolorowanie pakietow systemow");
        transaction.Start();

        foreach (SystemColorOption option in selectedOptions)
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

            result.ProcessedSystemsCount++;
            result.Messages.Add($"Zastosowano kolor dla systemu: {option.SystemName}");
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
        string ruleValue = option.IsVentilation ? option.SystemName.Split('(')[0].Trim() : option.SystemName;

        FilterRule rule = ParameterFilterRuleFactory.CreateContainsRule(parameterId, ruleValue);
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
}
