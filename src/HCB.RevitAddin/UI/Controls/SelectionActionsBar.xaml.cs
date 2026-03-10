using System.Windows;
using System.Windows.Controls;

namespace HCB.RevitAddin.UI.Controls
{
    public partial class SelectionActionsBar : UserControl
    {
        public static readonly DependencyProperty LeftButtonTextProperty =
            DependencyProperty.Register(
                nameof(LeftButtonText),
                typeof(string),
                typeof(SelectionActionsBar),
                new PropertyMetadata("All"));

        public static readonly DependencyProperty RightButtonTextProperty =
            DependencyProperty.Register(
                nameof(RightButtonText),
                typeof(string),
                typeof(SelectionActionsBar),
                new PropertyMetadata("Clear"));

        public static readonly RoutedEvent LeftButtonClickEvent =
            EventManager.RegisterRoutedEvent(
                nameof(LeftButtonClick),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(SelectionActionsBar));

        public static readonly RoutedEvent RightButtonClickEvent =
            EventManager.RegisterRoutedEvent(
                nameof(RightButtonClick),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(SelectionActionsBar));

        public SelectionActionsBar()
        {
            InitializeComponent();
        }

        public string LeftButtonText
        {
            get => (string)GetValue(LeftButtonTextProperty);
            set => SetValue(LeftButtonTextProperty, value);
        }

        public string RightButtonText
        {
            get => (string)GetValue(RightButtonTextProperty);
            set => SetValue(RightButtonTextProperty, value);
        }

        public event RoutedEventHandler LeftButtonClick
        {
            add => AddHandler(LeftButtonClickEvent, value);
            remove => RemoveHandler(LeftButtonClickEvent, value);
        }

        public event RoutedEventHandler RightButtonClick
        {
            add => AddHandler(RightButtonClickEvent, value);
            remove => RemoveHandler(RightButtonClickEvent, value);
        }

        private void LeftButton_OnClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(LeftButtonClickEvent, this));
        }

        private void RightButton_OnClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(RightButtonClickEvent, this));
        }
    }
}
