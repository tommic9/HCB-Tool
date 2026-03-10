using System.Windows;
using System.Windows.Controls;

namespace HCB.RevitAddin.UI.Controls
{
    public partial class DialogFooterBar : UserControl
    {
        public static readonly DependencyProperty StatusTextProperty =
            DependencyProperty.Register(
                nameof(StatusText),
                typeof(string),
                typeof(DialogFooterBar),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty PrimaryButtonTextProperty =
            DependencyProperty.Register(
                nameof(PrimaryButtonText),
                typeof(string),
                typeof(DialogFooterBar),
                new PropertyMetadata("Zastosuj"));

        public static readonly DependencyProperty SecondaryButtonTextProperty =
            DependencyProperty.Register(
                nameof(SecondaryButtonText),
                typeof(string),
                typeof(DialogFooterBar),
                new PropertyMetadata("Anuluj"));

        public static readonly RoutedEvent PrimaryButtonClickEvent =
            EventManager.RegisterRoutedEvent(
                nameof(PrimaryButtonClick),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(DialogFooterBar));

        public static readonly RoutedEvent SecondaryButtonClickEvent =
            EventManager.RegisterRoutedEvent(
                nameof(SecondaryButtonClick),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(DialogFooterBar));

        public DialogFooterBar()
        {
            InitializeComponent();
        }

        public string StatusText
        {
            get => (string)GetValue(StatusTextProperty);
            set => SetValue(StatusTextProperty, value);
        }

        public string PrimaryButtonText
        {
            get => (string)GetValue(PrimaryButtonTextProperty);
            set => SetValue(PrimaryButtonTextProperty, value);
        }

        public string SecondaryButtonText
        {
            get => (string)GetValue(SecondaryButtonTextProperty);
            set => SetValue(SecondaryButtonTextProperty, value);
        }

        public event RoutedEventHandler PrimaryButtonClick
        {
            add => AddHandler(PrimaryButtonClickEvent, value);
            remove => RemoveHandler(PrimaryButtonClickEvent, value);
        }

        public event RoutedEventHandler SecondaryButtonClick
        {
            add => AddHandler(SecondaryButtonClickEvent, value);
            remove => RemoveHandler(SecondaryButtonClickEvent, value);
        }

        private void PrimaryButton_OnClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(PrimaryButtonClickEvent, this));
        }

        private void SecondaryButton_OnClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(SecondaryButtonClickEvent, this));
        }
    }
}
