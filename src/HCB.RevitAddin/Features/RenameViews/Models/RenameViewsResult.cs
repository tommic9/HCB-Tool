using System.Collections.Generic;

namespace HCB.RevitAddin.Features.RenameViews.Models;

public sealed class RenameViewsResult
{
    public int ProcessedCount { get; set; }

    public int RenamedCount { get; set; }

    public List<string> Messages { get; } = new();
}
