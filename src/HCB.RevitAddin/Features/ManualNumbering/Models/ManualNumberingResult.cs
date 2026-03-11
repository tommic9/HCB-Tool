using System.Collections.Generic;

namespace HCB.RevitAddin.Features.ManualNumbering.Models;

public sealed class ManualNumberingResult
{
    public int UpdatedCount { get; set; }

    public int SkippedCount { get; set; }

    public List<string> Messages { get; } = [];
}
