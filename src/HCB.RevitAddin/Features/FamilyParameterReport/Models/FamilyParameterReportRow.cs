namespace HCB.RevitAddin.Features.FamilyParameterReport.Models;

public sealed class FamilyParameterReportRow
{
    public string FilePath { get; init; } = string.Empty;

    public string FamilyName { get; init; } = string.Empty;

    public int TypeCount { get; init; }

    public string ParameterName { get; init; } = string.Empty;

    public bool IsShared { get; init; }

    public bool IsInstance { get; init; }

    public bool IsBuiltIn { get; init; }

    public bool CanRename { get; init; }

    public string ParameterSource { get; init; } = string.Empty;

    public string GroupTypeId { get; init; } = string.Empty;

    public string SpecTypeId { get; init; } = string.Empty;

    public string Formula { get; init; } = string.Empty;
}
