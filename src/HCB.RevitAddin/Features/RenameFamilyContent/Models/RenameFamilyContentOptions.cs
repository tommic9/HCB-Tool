namespace HCB.RevitAddin.Features.RenameFamilyContent.Models;

public sealed class RenameFamilyContentOptions
{
    public string Prefix { get; init; } = string.Empty;

    public string Find { get; init; } = string.Empty;

    public string Replace { get; init; } = string.Empty;

    public string Suffix { get; init; } = string.Empty;

    public bool SaveAsCopy { get; init; } = true;

    public string OutputFolderPath { get; init; } = string.Empty;

    public IReadOnlyList<string> TargetParameterKeys { get; init; } = [];
}
