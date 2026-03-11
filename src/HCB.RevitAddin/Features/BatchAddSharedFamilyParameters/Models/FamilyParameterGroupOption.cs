using Autodesk.Revit.DB;

namespace HCB.RevitAddin.Features.BatchAddSharedFamilyParameters.Models;

public sealed class FamilyParameterGroupOption
{
    public string Key { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public ForgeTypeId GroupTypeId { get; init; } = Autodesk.Revit.DB.GroupTypeId.Data;

    public override string ToString()
    {
        return Label;
    }
}

