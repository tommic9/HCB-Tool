using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using HCB.RevitAddin.Features.SystemAssigner.Models;

namespace HCB.RevitAddin.Features.SystemAssigner;

public sealed class SystemAssignerService
{
    public IReadOnlyList<string> GetPropagatableParameterNames(IReadOnlyList<Element> selectedEquipment)
    {
        if (selectedEquipment.Count == 0)
        {
            return [];
        }

        HashSet<string>? commonNames = null;
        foreach (Element equipment in selectedEquipment)
        {
            HashSet<string> elementNames = equipment.Parameters
                .Cast<Parameter>()
                .Where(parameter => !parameter.IsReadOnly && parameter.StorageType == StorageType.String)
                .Select(parameter => parameter.Definition?.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .ToHashSet(System.StringComparer.CurrentCultureIgnoreCase);

            commonNames = commonNames == null
                ? elementNames
                : commonNames.Intersect(elementNames, System.StringComparer.CurrentCultureIgnoreCase).ToHashSet(System.StringComparer.CurrentCultureIgnoreCase);
        }

        return commonNames?
            .OrderBy(name => name, System.StringComparer.CurrentCultureIgnoreCase)
            .ToList() ?? [];
    }

    public SystemAssignerResult Apply(Document document, IReadOnlyList<Element> selectedEquipment, string parameterName)
    {
        SystemAssignerResult result = new();

        using Transaction transaction = new(document, $"Propagate {parameterName}");
        transaction.Start();

        foreach (Element equipment in selectedEquipment)
        {
            result.ProcessedEquipmentCount++;
            string? parameterValue = GetParameterStringValue(equipment, parameterName);
            if (string.IsNullOrWhiteSpace(parameterValue))
            {
                result.Messages.Add($"Pominieto urzadzenie {equipment.Id.Value}: brak wartosci parametru {parameterName}.");
                continue;
            }

            IReadOnlyList<MEPSystem> systems = GetAssignedSystems(equipment);
            if (systems.Count == 0)
            {
                result.Messages.Add($"Pominieto urzadzenie {equipment.Id.Value}: brak dobrze polaczonego systemu MEP.");
                continue;
            }

            foreach (MEPSystem mepSystem in systems)
            {
                result.ProcessedSystemCount++;

                foreach (Element element in GetSystemElements(mepSystem))
                {
                    Parameter? parameter = element.LookupParameter(parameterName);
                    if (parameter == null || parameter.IsReadOnly || parameter.StorageType != StorageType.String)
                    {
                        result.Messages.Add($"Element {element.Id.Value}: brak parametru {parameterName}.");
                        continue;
                    }

                    parameter.Set(parameterValue);
                    result.ChangedCount++;
                }
            }
        }

        transaction.Commit();
        return result;
    }

    private static string? GetParameterStringValue(Element element, string parameterName)
    {
        Parameter? parameter = element.LookupParameter(parameterName);
        if (parameter == null || !parameter.HasValue)
        {
            return null;
        }

        return parameter.StorageType == StorageType.String ? parameter.AsString() : parameter.AsValueString();
    }

    private static IReadOnlyList<MEPSystem> GetAssignedSystems(Element element)
    {
        if (element is not FamilyInstance familyInstance)
        {
            return [];
        }

        try
        {
            ConnectorSet? connectors = familyInstance.MEPModel?.ConnectorManager?.Connectors;
            if (connectors == null)
            {
                return [];
            }

            return connectors
                .Cast<Connector>()
                .Select(connector => connector.MEPSystem)
                .Where(system => system != null)
                .Where(IsWellConnected)
                .Distinct(new MepSystemIdComparer())
                .Cast<MEPSystem>()
                .OrderByDescending(system => system.Elements.Size)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static IEnumerable<Element> GetSystemElements(MEPSystem system)
    {
        Dictionary<long, Element> elementsById = new();

        void AddElement(Element? element)
        {
            if (element != null)
            {
                elementsById[element.Id.Value] = element;
            }
        }

        AddElement(system.BaseEquipment);

        foreach (Element element in system.Elements.Cast<Element>())
        {
            AddElement(element);
        }

        if (system is MechanicalSystem mechanicalSystem)
        {
            foreach (Element element in mechanicalSystem.DuctNetwork.Cast<Element>())
            {
                AddElement(element);
            }
        }
        else if (system is PipingSystem pipingSystem)
        {
            foreach (Element element in pipingSystem.PipingNetwork.Cast<Element>())
            {
                AddElement(element);
            }
        }

        return elementsById.Values;
    }

    private static bool IsWellConnected(MEPSystem? system)
    {
        return system switch
        {
            MechanicalSystem mechanicalSystem => mechanicalSystem.IsWellConnected,
            PipingSystem pipingSystem => pipingSystem.IsWellConnected,
            _ => false
        };
    }

    private sealed class MepSystemIdComparer : IEqualityComparer<MEPSystem>
    {
        public bool Equals(MEPSystem? x, MEPSystem? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.Id.Value == y.Id.Value;
        }

        public int GetHashCode(MEPSystem obj) => obj.Id.Value.GetHashCode();
    }
}
