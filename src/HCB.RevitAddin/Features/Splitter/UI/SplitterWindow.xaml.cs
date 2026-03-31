using System.Globalization;
using System.Windows;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.Splitter.Models;

namespace HCB.RevitAddin.Features.Splitter.UI;

public partial class SplitterWindow : Window
{
    public SplitterWindow()
    {
        InitializeComponent();
        SegmentLengthTextBox.Text = "1500";
        DuctsCheckBox.IsChecked = true;
        PipesCheckBox.IsChecked = true;
        FooterBar.StatusText = "Podaj dlugosc odcinka w milimetrach i wybierz zakres elementow.";
        Loaded += (_, _) => SegmentLengthTextBox.Focus();
    }

    public SplitterOptions? SelectedOptions { get; private set; }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryParseLength(out double lengthMillimeters))
        {
            FooterBar.StatusText = "Podaj poprawna dodatnia dlugosc w mm.";
            return;
        }

        bool splitDucts = DuctsCheckBox.IsChecked == true;
        bool splitPipes = PipesCheckBox.IsChecked == true;
        if (!splitDucts && !splitPipes)
        {
            FooterBar.StatusText = "Wybierz przynajmniej jeden typ elementow.";
            return;
        }

        SelectedOptions = new SplitterOptions
        {
            SegmentLengthMillimeters = lengthMillimeters,
            SegmentLengthInternal = UnitUtils.ConvertToInternalUnits(lengthMillimeters, UnitTypeId.Millimeters),
            SplitDucts = splitDucts,
            SplitPipes = splitPipes
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private bool TryParseLength(out double lengthMillimeters)
    {
        string text = SegmentLengthTextBox.Text.Trim();
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out lengthMillimeters) ||
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out lengthMillimeters))
        {
            return lengthMillimeters > 0d;
        }

        lengthMillimeters = 0d;
        return false;
    }
}
