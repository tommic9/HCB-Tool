using System.Collections.Generic;

namespace HCB.RevitAddin.Features.MassOfDuctsFittings.Models;

public sealed class MassOfDuctsFittingsResult
{
    public int UpdatedCount { get; set; }

    public int SkippedCount { get; set; }

    public List<string> Messages { get; } = new();
}
