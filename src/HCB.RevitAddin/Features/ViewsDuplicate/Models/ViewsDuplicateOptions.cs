namespace HCB.RevitAddin.Features.ViewsDuplicate.Models;

public sealed class ViewsDuplicateOptions
{
    public int CopiesCount { get; init; }

    public string DuplicateMode { get; init; } = "WithDetailing";
}
