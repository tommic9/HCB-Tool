namespace HCB.RevitAddin.Features.Splitter.Models;

public sealed class SplitterResult
{
    public bool Success { get; init; }

    public bool HasChanges { get; init; }

    public long ElementId { get; init; }

    public int SplitCount { get; init; }

    public int FittingCount { get; init; }

    public string Message { get; init; } = string.Empty;
}
