namespace HCB.RevitAddin.Features.BatchAddSharedFamilyParameters.Models;

public sealed class BatchAddSharedFamilyParametersOptions
{
    public IReadOnlyList<string> SelectedParameterNames { get; init; } = [];

    public bool IsInstance { get; init; }

    public string GroupKey { get; init; } = string.Empty;
}
