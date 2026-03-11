using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.ViewFiltersLegend.Models;

namespace HCB.RevitAddin.Features.ViewFiltersLegend.UI;

public partial class ViewFiltersLegendWindow : Window
{
    public ViewFiltersLegendWindow(IReadOnlyList<TextNoteType> textTypes)
    {
        InitializeComponent();
        TextTypeComboBox.ItemsSource = textTypes.Select(type => new TextTypeItem(type)).ToList();
        TextTypeComboBox.SelectedIndex = textTypes.Count > 0 ? 0 : -1;
        FooterBar.StatusText = "Wybierz typ tekstu i wymiary probek legendy.";
    }

    public ViewFiltersLegendOptions Options => new()
    {
        TextTypeId = (TextTypeComboBox.SelectedItem as TextTypeItem)?.TextType.Id.Value ?? 0,
        SampleWidthMillimeters = ParseDouble(RegionWidthTextBox.Text),
        SampleHeightMillimeters = ParseDouble(RegionHeightTextBox.Text),
        SpacingMillimeters = ParseDouble(SpacingTextBox.Text),
        LineLengthMillimeters = ParseDouble(LineWidthTextBox.Text),
        IncludeProjectionLine = ProjectionLineCheckBox.IsChecked == true,
        IncludeCutLine = CutLineCheckBox.IsChecked == true,
        IncludeSurfaceFill = SurfaceFillCheckBox.IsChecked == true,
        IncludeCutFill = CutFillCheckBox.IsChecked == true,
        IncludeFilterName = FilterNameCheckBox.IsChecked == true
    };

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Options.TextTypeId == 0)
        {
            FooterBar.StatusText = "Wybierz typ tekstu.";
            return;
        }

        if (Options.SampleWidthMillimeters <= 0 ||
            Options.SampleHeightMillimeters <= 0 ||
            Options.SpacingMillimeters <= 0 ||
            Options.LineLengthMillimeters <= 0)
        {
            FooterBar.StatusText = "Wszystkie wymiary musza byc dodatnie.";
            return;
        }

        if (!Options.IncludeProjectionLine &&
            !Options.IncludeCutLine &&
            !Options.IncludeSurfaceFill &&
            !Options.IncludeCutFill &&
            !Options.IncludeFilterName)
        {
            FooterBar.StatusText = "Wybierz co najmniej jeden element legendy.";
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static double ParseDouble(string text)
    {
        string normalized = (text ?? string.Empty).Trim().Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : 0;
    }

    private sealed class TextTypeItem(TextNoteType textType)
    {
        public TextNoteType TextType { get; } = textType;

        public override string ToString()
        {
            return TextType.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME)?.AsString() ?? TextType.Name;
        }
    }
}
