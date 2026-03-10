using System.Collections.Generic;
using System.Windows;

namespace HCB.RevitAddin.Features.StylePreview.UI
{
    public partial class StylePreviewWindow : Window
    {
        public StylePreviewWindow()
        {
            InitializeComponent();
            DataContext = new StylePreviewViewModel();
        }

        private sealed class StylePreviewViewModel
        {
            public IReadOnlyList<string> ListItems { get; } = new[]
            {
                "Primary content item",
                "Secondary content item",
                "Longer example row to test spacing",
                "Selected state sample"
            };
        }
    }
}
