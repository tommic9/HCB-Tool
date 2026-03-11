namespace HCB.RevitAddin.Features.ManualNumbering.Models;

public sealed class ManualNumberingOptions
{
    public string ParameterName { get; set; } = string.Empty;

    public int StartNumber { get; set; } = 1;

    public string Prefix { get; set; } = string.Empty;

    public string Suffix { get; set; } = string.Empty;
}
