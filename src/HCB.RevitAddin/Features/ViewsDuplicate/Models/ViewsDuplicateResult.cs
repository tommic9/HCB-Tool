using System.Collections.Generic;

namespace HCB.RevitAddin.Features.ViewsDuplicate.Models;

public sealed class ViewsDuplicateResult
{
    public int SourceCount { get; set; }

    public int CreatedCount { get; set; }

    public List<string> Messages { get; } = new();
}
