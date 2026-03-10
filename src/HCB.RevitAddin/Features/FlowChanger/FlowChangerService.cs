using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.FlowChanger.Models;

namespace HCB.RevitAddin.Features.FlowChanger;

public sealed class FlowChangerService
{
    private readonly Random _random = new();

    public IReadOnlyList<string> GetAvailableTargetParameters(Document document)
    {
        return GetAirTerminalsInActiveView(document)
            .SelectMany(terminal => terminal.Parameters.Cast<Parameter>())
            .Where(parameter =>
                parameter.Definition != null &&
                parameter.StorageType == StorageType.Double &&
                !parameter.IsReadOnly)
            .Select(parameter => parameter.Definition.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public FlowChangerResult UpdateActualFlow(Document document, string actualFlowParameterName)
    {
        List<Element> terminals = GetAirTerminalsInActiveView(document);
        FlowChangerResult result = new();

        using Transaction transaction = new(document, "FlowExchanger");
        transaction.Start();

        foreach (Element terminal in terminals)
        {
            result.ProcessedCount++;
            Parameter flowParameter = terminal.get_Parameter(BuiltInParameter.RBS_DUCT_FLOW_PARAM);
            Parameter actualFlowParameter = terminal.LookupParameter(actualFlowParameterName);

            if (flowParameter == null || actualFlowParameter == null || actualFlowParameter.IsReadOnly)
            {
                result.Messages.Add($"Pominieto element {terminal.Id.Value}: brak parametru przeplywu lub parametru docelowego.");
                continue;
            }

            double currentFlow = UnitUtils.ConvertFromInternalUnits(flowParameter.AsDouble(), UnitTypeId.CubicMetersPerHour);
            double variation = _random.NextDouble() * 0.04 + 0.985;
            double updatedFlow = Math.Floor(currentFlow * variation);
            double updatedInternal = UnitUtils.ConvertToInternalUnits(updatedFlow, UnitTypeId.CubicMetersPerHour);

            actualFlowParameter.Set(updatedInternal);
            result.UpdatedCount++;
            result.Messages.Add($"Element {terminal.Id.Value}: {currentFlow:0} -> {updatedFlow:0} m3/h");
        }

        transaction.Commit();
        return result;
    }

    private static List<Element> GetAirTerminalsInActiveView(Document document)
    {
        return new FilteredElementCollector(document, document.ActiveView.Id)
            .OfCategory(BuiltInCategory.OST_DuctTerminal)
            .WhereElementIsNotElementType()
            .ToElements()
            .ToList();
    }
}
