using System.Collections.Generic;

namespace HCB.RevitAddin.Features.RenameSheets.Models;

public sealed class RenameSheetsResult
{
    public int ProcessedCount { get; set; }

    public int RenamedCount { get; set; }

    public List<string> Messages { get; } = new();
}
