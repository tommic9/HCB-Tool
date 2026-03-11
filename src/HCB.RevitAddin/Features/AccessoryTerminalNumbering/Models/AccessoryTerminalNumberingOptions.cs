namespace HCB.RevitAddin.Features.AccessoryTerminalNumbering.Models;

public sealed class AccessoryTerminalNumberingOptions
{
    public string TargetParameterName { get; set; } = string.Empty;

    public int StartNumber { get; set; } = 1;

    public string DuctAccessoryPrefix { get; set; } = string.Empty;

    public string PipeAccessoryPrefix { get; set; } = string.Empty;

    public string TerminalPrefix { get; set; } = "AT";

    public string AccessoryTypeParameterName { get; set; } = string.Empty;
}
