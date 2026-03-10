using System.Collections.Generic;

namespace HCB.RevitAddin.Features.SpaceToElement.Models;

public sealed class SpaceToElementResult
{
    public int UpdatedCount { get; set; }

    public int SkippedCount { get; set; }

    public int ErrorCount { get; set; }

    public List<string> Messages { get; } = new();
}
