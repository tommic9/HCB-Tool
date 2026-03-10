using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.ColorUniqueSystems.Models;

namespace HCB.RevitAddin.Features.ColorUniqueSystems;

public sealed class ColorUniqueSystemsService
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

    public IReadOnlyList<SystemColorOption> GetVentilationSystems() => VentilationSystems;

    public IReadOnlyList<SystemColorOption> GetPipingSystems() => PipingSystems;

    public ColorUniqueSystemsResult Apply(Document document, View view, IEnumerable<SystemColorOption> selectedOptions, bool overrideDisplayLines)
    {
        List<SystemColorOption> options = selectedOptions.ToList();
        ColorUniqueSystemsResult result = new();

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

        FilterRule rule = ParameterFilterRuleFactory.CreateContainsRule(
            parameterId,
            option.IsVentilation ? option.SystemName.Split('(')[0].Trim() : option.SystemName);

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
