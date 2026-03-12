using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.FittingsAngle.Models;

namespace HCB.RevitAddin.Features.FittingsAngle;

public sealed class FittingsAngleService
{
    private const string TargetSharedParameterName = "HC_Kąt";
    public const string NoExtraParameterOption = "__NONE__";
    private static readonly string[] PredefinedAngleParameters = ["Angle", "angle", "w", "arc", "RSen_P_c01_angle"];

    private static readonly IReadOnlyDictionary<string, BuiltInCategory> CategoryOptions = new Dictionary<string, BuiltInCategory>
    {
        ["Pipe Fittings"] = BuiltInCategory.OST_PipeFitting,
        ["Duct Fittings"] = BuiltInCategory.OST_DuctFitting,
        ["Cable Tray Fittings"] = BuiltInCategory.OST_CableTrayFitting,
        ["Conduit Fittings"] = BuiltInCategory.OST_ConduitFitting
    };

    public IReadOnlyList<string> GetCategoryNames() => CategoryOptions.Keys.OrderBy(name => name).ToList();

    public IReadOnlyList<string> GetAvailableAngleParameterNames(Document document, IEnumerable<ElementId> selectedElementIds, IReadOnlyList<string> selectedCategoryNames)
    {
        List<BuiltInCategory> selectedCategories = selectedCategoryNames
            .Where(CategoryOptions.ContainsKey)
            .Select(name => CategoryOptions[name])
            .ToList();

        if (selectedCategories.Count == 0)
        {
            return [];
        }

        return GetCandidateElements(document, selectedElementIds, selectedCategories)
            .SelectMany(element => element.Parameters.Cast<Parameter>())
            .Where(parameter => parameter.StorageType == StorageType.Double)
            .Where(IsAngleParameter)
            .Select(parameter => parameter.Definition?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Where(name => !string.Equals(name, TargetSharedParameterName, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .Cast<string>()
            .ToList();
    }

    public FittingsAngleResult Apply(Document document, IEnumerable<ElementId> selectedElementIds, IReadOnlyList<string> selectedCategoryNames, string? extraAngleParameterName)
    {
        List<BuiltInCategory> selectedCategories = selectedCategoryNames
            .Where(CategoryOptions.ContainsKey)
            .Select(name => CategoryOptions[name])
            .ToList();

        if (selectedCategories.Count == 0)
        {
            return new FittingsAngleResult();
        }

        List<string> angleParameters = [];
        if (!string.IsNullOrWhiteSpace(extraAngleParameterName) && !string.Equals(extraAngleParameterName, NoExtraParameterOption, StringComparison.Ordinal))
        {
            angleParameters.Add(extraAngleParameterName.Trim());
        }

        angleParameters.AddRange(PredefinedAngleParameters);
        angleParameters = angleParameters.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        List<Element> candidates = GetCandidateElements(document, selectedElementIds, selectedCategories)
            .Where(element => TryGetSourceAngle(element, angleParameters, out _))
            .ToList();

        FittingsAngleResult result = new()
        {
            CandidateCount = candidates.Count
        };

        using Transaction transaction = new(document, "Fittings Angle");
        transaction.Start();

        foreach (Element element in candidates)
        {
            if (!TryGetSourceAngle(element, angleParameters, out double sourceAngle))
            {
                result.MissingSourceCount++;
                continue;
            }

            Parameter? targetParameter = element.LookupParameter(TargetSharedParameterName);
            if (targetParameter == null || targetParameter.IsReadOnly || targetParameter.StorageType != StorageType.Double)
            {
                result.MissingTargetCount++;
                result.FailedElementIds.Add(element.Id.Value);
                continue;
            }

            double roundedAngle = RoundAngle(sourceAngle, IsRectangularDuctFitting(element) ? 1.0 : 5.0);

            try
            {
                targetParameter.Set(roundedAngle);
                result.UpdatedCount++;
            }
            catch
            {
                result.MissingTargetCount++;
                result.FailedElementIds.Add(element.Id.Value);
            }
        }

        if (result.UpdatedCount > 0)
        {
            transaction.Commit();
        }
        else
        {
            transaction.RollBack();
        }

        return result;
    }

    private static IEnumerable<Element> GetCandidateElements(Document document, IEnumerable<ElementId> selectedElementIds, IReadOnlyList<BuiltInCategory> selectedCategories)
    {
        HashSet<long> selectedIds = selectedElementIds.Select(id => id.Value).ToHashSet();
        bool useSelection = selectedIds.Count > 0;

        ElementFilter filter = selectedCategories.Count == 1
            ? new ElementCategoryFilter(selectedCategories[0])
            : new LogicalOrFilter(selectedCategories.Select(category => new ElementCategoryFilter(category)).Cast<ElementFilter>().ToList());

        return new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .WherePasses(filter)
            .ToElements()
            .Where(element => !useSelection || selectedIds.Contains(element.Id.Value));
    }

    private static bool TryGetSourceAngle(Element element, IEnumerable<string> parameterNames, out double angle)
    {
        foreach (string parameterName in parameterNames)
        {
            Parameter? parameter = element.LookupParameter(parameterName);
            if (parameter?.HasValue == true &&
                parameter.StorageType == StorageType.Double &&
                IsAngleParameter(parameter))
            {
                angle = parameter.AsDouble();
                return true;
            }
        }

        angle = 0;
        return false;
    }

    private static bool IsAngleParameter(Parameter parameter)
    {
        try
        {
            Definition? definition = parameter.Definition;
            if (definition == null)
            {
                return false;
            }

            ForgeTypeId dataType = definition.GetDataType();
            return dataType == SpecTypeId.Angle;
        }
        catch
        {
            return false;
        }
    }

    private static double RoundAngle(double radiansValue, double degreesStep)
    {
        double degrees = radiansValue * 180.0 / Math.PI;
        double roundedDegrees = Math.Round(degrees / degreesStep) * degreesStep;
        return roundedDegrees * Math.PI / 180.0;
    }

    private static bool IsRectangularDuctFitting(Element element)
    {
        if (element.Category?.Id.Value != (long)BuiltInCategory.OST_DuctFitting || element is not FamilyInstance familyInstance)
        {
            return false;
        }

        try
        {
            ConnectorSet? connectors = familyInstance.MEPModel?.ConnectorManager?.Connectors;
            return connectors != null
                   && connectors.Cast<Connector>().Any()
                   && connectors.Cast<Connector>().All(connector => connector.Shape == ConnectorProfileType.Rectangular);
        }
        catch
        {
            return false;
        }
    }
}
