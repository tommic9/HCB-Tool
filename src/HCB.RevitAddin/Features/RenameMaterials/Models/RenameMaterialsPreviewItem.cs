namespace HCB.RevitAddin.Features.RenameMaterials.Models;

public sealed record RenameMaterialsPreviewItem(
    string CurrentName,
    string NewName,
    string Status);
