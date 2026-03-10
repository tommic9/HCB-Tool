namespace HCB.RevitAddin.Features.RenameMaterials.Models;

public sealed class RenameMaterialsResult
{
    public int RenamedCount { get; set; }

    public int SkippedCount { get; set; }

    public int UnchangedCount { get; set; }
}
