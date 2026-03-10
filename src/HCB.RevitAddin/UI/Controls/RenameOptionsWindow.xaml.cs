using System.Windows;

namespace HCB.RevitAddin.UI.Controls;

public partial class RenameOptionsWindow : Window
{
    public RenameOptionsWindow(string title, RenameOptionsMode mode)
    {
        InitializeComponent();

        Title = title;
        TitleTextBlock.Text = title;
        FooterBar.StatusText = "Uzupelnij pola, ktore maja wplywac na nowe nazwy.";

        bool isSheetsMode = mode == RenameOptionsMode.Sheets;

        SetVisibility(NumberPrefixLabel, isSheetsMode);
        SetVisibility(NumberPrefixTextBox, isSheetsMode);
        SetVisibility(NumberFindLabel, isSheetsMode);
        SetVisibility(NumberFindTextBox, isSheetsMode);
        SetVisibility(NumberReplaceLabel, isSheetsMode);
        SetVisibility(NumberReplaceTextBox, isSheetsMode);

        Height = isSheetsMode ? 470 : 400;
        MinHeight = isSheetsMode ? 430 : 360;
    }

    public RenameOptions Options => new()
    {
        NumberPrefix = NumberPrefixTextBox.Text,
        NumberFind = NumberFindTextBox.Text,
        NumberReplace = NumberReplaceTextBox.Text,
        Prefix = PrefixTextBox.Text,
        Find = FindTextBox.Text,
        Replace = ReplaceTextBox.Text,
        Suffix = SuffixTextBox.Text
    };

    private static void SetVisibility(UIElement element, bool isVisible)
    {
        element.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public enum RenameOptionsMode
{
    Views,
    Sheets
}

public sealed class RenameOptions
{
    public string NumberPrefix { get; init; } = string.Empty;

    public string NumberFind { get; init; } = string.Empty;

    public string NumberReplace { get; init; } = string.Empty;

    public string Prefix { get; init; } = string.Empty;

    public string Find { get; init; } = string.Empty;

    public string Replace { get; init; } = string.Empty;

    public string Suffix { get; init; } = string.Empty;
}
