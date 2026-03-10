using System.Collections.Generic;

namespace HCB.RevitAddin.Features.Levels.Models;

public sealed class LevelsRenameResult
{
    public int TotalLevelsCount { get; set; }

    public int RenamedLevelsCount { get; set; }

    public int UnchangedLevelsCount { get; set; }

    public List<string> Messages { get; } = new();
}
