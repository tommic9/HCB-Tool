using System.Collections.Generic;

namespace HCB.RevitAddin.Features.LevelFromHVACElements.Models;

public sealed class LevelFromHvacResult
{
    public int UpdatedCount { get; set; }

    public int FailedCount { get; set; }

    public List<long> FailedElementIds { get; } = new();
}
