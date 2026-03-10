using System.Windows.Media;

namespace HCB.RevitAddin.UI.Controls;

public sealed class ColorSelectionItem
{
    public ColorSelectionItem(object value, string displayName, byte red, byte green, byte blue, string? filterGroup = null)
    {
        Value = value;
        DisplayName = displayName;
        FilterGroup = filterGroup;
        PreviewBrush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        PreviewBrush.Freeze();
    }

    public object Value { get; }

    public string DisplayName { get; }

    public string? FilterGroup { get; }

    public Brush PreviewBrush { get; }
}
