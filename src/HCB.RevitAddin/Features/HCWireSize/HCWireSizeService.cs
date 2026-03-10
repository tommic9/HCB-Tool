using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using HCB.RevitAddin.Features.HCWireSize.Models;

namespace HCB.RevitAddin.Features.HCWireSize;

public sealed class HCWireSizeService
{
    private const string TargetParameterName = "HC_WireSize";
    private const string WireSizeParameterName = "Wire Size";
    private const string PolesParameterName = "NumberOfPoles";
    private const string NeutralParameterName = "NeutralConductorsNumber";
    private const string GroundParameterName = "GroundConductorsSize";

    public HCWireSizeResult Apply(Document document)
    {
        List<ElectricalSystem> circuits = new FilteredElementCollector(document)
            .OfCategory(BuiltInCategory.OST_ElectricalCircuit)
            .WhereElementIsNotElementType()
            .Cast<ElectricalSystem>()
            .Where(circuit => circuit.SystemType == ElectricalSystemType.PowerCircuit)
            .ToList();

        HCWireSizeResult result = new();
        if (circuits.Count == 0)
        {
            result.Messages.Add("Brak obwodow elektrycznych typu Power.");
            return result;
        }

        using Transaction transaction = new(document, "HC Wire Size");
        transaction.Start();

        foreach (ElectricalSystem circuit in circuits)
        {
            Parameter? targetParameter = circuit.LookupParameter(TargetParameterName);
            if (targetParameter == null || targetParameter.IsReadOnly || targetParameter.StorageType != StorageType.String)
            {
                result.Messages.Add($"Obwod {circuit.Id.Value}: brak parametru HC_WireSize.");
                continue;
            }

            int? poles = GetInt(circuit.LookupParameter(PolesParameterName));
            int? neutralCount = GetInt(circuit.LookupParameter(NeutralParameterName));
            int? groundCount = GetInt(circuit.LookupParameter(GroundParameterName));
            string? wireSizeRaw = GetString(circuit.LookupParameter(WireSizeParameterName));

            if (poles == null)
            {
                result.Messages.Add($"Obwod {circuit.Id.Value}: brak NumberOfPoles.");
                continue;
            }

            int totalCores = (poles ?? 0) + (neutralCount ?? 0) + (groundCount ?? 0);
            if (totalCores <= 0)
            {
                result.Messages.Add($"Obwod {circuit.Id.Value}: suma zyl jest <= 0.");
                continue;
            }

            string? section = ExtractWireSectionAfterHash(wireSizeRaw);
            if (section == null)
            {
                result.Messages.Add($"Obwod {circuit.Id.Value}: nie mozna odczytac przekroju z Wire Size.");
                continue;
            }

            targetParameter.Set($"{totalCores}x{section} mm2");
            result.UpdatedCount++;
        }

        transaction.Commit();
        return result;
    }

    private static int? GetInt(Parameter? parameter)
    {
        if (parameter == null)
        {
            return null;
        }

        try
        {
            return parameter.StorageType switch
            {
                StorageType.Integer => parameter.AsInteger(),
                StorageType.Double => (int)System.Math.Round(parameter.AsDouble()),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? GetString(Parameter? parameter)
    {
        if (parameter == null)
        {
            return null;
        }

        string? value = parameter.AsString();
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        value = parameter.AsValueString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? ExtractWireSectionAfterHash(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        Match match = Regex.Match(rawValue, @"#\s*([0-9]+(?:[\.,][0-9]+)?)");
        if (!match.Success)
        {
            return null;
        }

        return match.Groups[1].Value.Replace(",", ".");
    }
}
