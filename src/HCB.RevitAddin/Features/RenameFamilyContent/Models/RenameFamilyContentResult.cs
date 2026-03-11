using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;

namespace HCB.RevitAddin.Features.RenameFamilyContent.Models;

public sealed class RenameFamilyContentResult
{
    public IReadOnlyList<WithoutOpenOperationLogEntry> Entries { get; init; } = [];

    public int SuccessCount => Entries.Count(entry => entry.Status == WithoutOpenOperationStatus.Success);

    public int SkippedCount => Entries.Count(entry => entry.Status == WithoutOpenOperationStatus.Skipped);

    public int FailedCount => Entries.Count(entry => entry.Status == WithoutOpenOperationStatus.Failed);
}
