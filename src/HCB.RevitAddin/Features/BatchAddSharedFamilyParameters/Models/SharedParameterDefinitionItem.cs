namespace HCB.RevitAddin.Features.BatchAddSharedFamilyParameters.Models;

public sealed class SharedParameterDefinitionItem
{
    public string Name { get; init; } = string.Empty;

    public string GroupName { get; init; } = string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(GroupName)
        ? Name
        : $"{Name} ({GroupName})";
}
