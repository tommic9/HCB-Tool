using System.Windows;

namespace HCB.RevitAddin.UI.Controls;

public partial class SingleValueInputWindow : Window
{
    private readonly bool _allowEmpty;

    public SingleValueInputWindow(string title, string prompt, string initialValue, string confirmButtonText, bool allowEmpty = false)
    {
        InitializeComponent();

        _allowEmpty = allowEmpty;
        Title = title;
        TitleTextBlock.Text = title;
        PromptTextBlock.Text = prompt;
        ValueTextBox.Text = initialValue;
        FooterBar.PrimaryButtonText = confirmButtonText;
        FooterBar.StatusText = "Wprowadz wartosc i zatwierdz.";
        Loaded += (_, _) => ValueTextBox.Focus();
    }

    public string EnteredValue => ValueTextBox.Text.Trim();

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_allowEmpty && string.IsNullOrWhiteSpace(EnteredValue))
        {
            FooterBar.StatusText = "Wartosc nie moze byc pusta.";
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
}
