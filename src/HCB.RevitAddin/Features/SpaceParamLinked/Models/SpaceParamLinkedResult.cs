using System.Collections.Generic;

namespace HCB.RevitAddin.Features.SpaceParamLinked.Models;

public sealed class SpaceParamLinkedResult
{
    public int ProcessedCount { get; set; }

    public int UpdatedCount { get; set; }

    public int NotFoundCount { get; set; }

    public List<string> Messages { get; } = new();
}
