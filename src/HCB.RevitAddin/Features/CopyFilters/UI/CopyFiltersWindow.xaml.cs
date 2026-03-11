using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.CopyFilters.Models;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.CopyFilters.UI
{
    public partial class CopyFiltersWindow : Window
    {
        private readonly UIApplication _uiApplication;
        private readonly CopyFiltersService _service = new CopyFiltersService();
        private List<ViewListItem> _allViews = new List<ViewListItem>();
        private List<ParameterFilterElement> _currentFilters = new List<ParameterFilterElement>();

        public CopyFiltersWindow(UIApplication uiApplication)
        {
            _uiApplication = uiApplication;
            InitializeComponent();
            LoadViews();
        }

        private Document Document => _uiApplication.ActiveUIDocument.Document;

        private void LoadViews()
        {
            _allViews = _service.GetSupportedViews(Document)
                .Select(view => new ViewListItem(view))
                .ToList();

            InitializeViewFilters();
            ApplySourceFilters();
            ApplyDestinationFilters();
            SetStatus(_allViews.Count == 0
                ? "Nie znaleziono widoków ani szablonów obsługujących filtry."
                : "Wybierz widok źródłowy, aby wczytać jego filtry.");
        }

        private void SourceViewsListBox_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ViewListItem? selectedSource = SourceViewsListBox.SelectedItem as ViewListItem;
            if (selectedSource == null)
            {
                FiltersListBox.ItemsSource = null;
                _currentFilters.Clear();
                SetStatus("Wybierz widok źródłowy, aby wczytać jego filtry.");
                return;
            }

            _currentFilters = _service.GetFiltersForView(Document, selectedSource.View).ToList();
            FiltersListBox.ItemsSource = _currentFilters;
            SetStatus(_currentFilters.Count == 0
                ? "Wybrany widok nie ma przypisanych filtrów."
                : $"Wczytano {_currentFilters.Count} filtrów z widoku '{selectedSource.DisplayName}'.");

            RemoveSourceFromDestinations(selectedSource.Id);
        }

        private void CopyButton_OnClick(object sender, RoutedEventArgs e)
        {
            ViewListItem? selectedSource = SourceViewsListBox.SelectedItem as ViewListItem;
            List<ParameterFilterElement> selectedFilters = FiltersListBox.SelectedItems.Cast<ParameterFilterElement>().ToList();
            List<View> destinationViews = DestinationViewsListBox.SelectedItems.Cast<ViewListItem>().Select(item => item.View).ToList();

            if (selectedSource == null)
            {
                TaskDialog.Show("Kopiowanie filtrów", "Wybierz widok lub szablon źródłowy.");
                return;
            }

            if (selectedFilters.Count == 0)
            {
                TaskDialog.Show("Kopiowanie filtrów", "Wybierz co najmniej jeden filtr do skopiowania.");
                return;
            }

            if (destinationViews.Count == 0)
            {
                TaskDialog.Show("Kopiowanie filtrów", "Wybierz co najmniej jeden widok lub szablon docelowy.");
                return;
            }

            ExistingFilterAction action = ExistingFilterAction.Overwrite;
            if (_service.HasConflicts(destinationViews, selectedFilters.Select(filter => filter.Id)))
            {
                TaskDialog dialog = new TaskDialog("Kopiowanie filtrów")
                {
                    MainInstruction = "Część widoków docelowych ma już wybrane filtry.",
                    MainContent = "Wybierz sposób obsługi istniejących ustawień filtrów.",
                    CommonButtons = TaskDialogCommonButtons.Cancel
                };
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Nadpisz istniejące ustawienia filtrów");
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Pomiń istniejące filtry");

                TaskDialogResult dialogResult = dialog.Show();
                if (dialogResult == TaskDialogResult.Cancel)
                {
                    return;
                }

                action = dialogResult == TaskDialogResult.CommandLink2
                    ? ExistingFilterAction.Skip
                    : ExistingFilterAction.Overwrite;
            }

            try
            {
                CopyFiltersResult result = _service.CopyFilters(
                    Document,
                    selectedSource.View,
                    selectedFilters,
                    destinationViews,
                    action);

                ShowResult(result);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Kopiowanie filtrów", $"Nie udało się skopiować filtrów.\n\n{ex.Message}");
            }
        }

        private void ShowResult(CopyFiltersResult result)
        {
            IEnumerable<string> messages = result.Messages.Take(12);
            string details = string.Join(Environment.NewLine, messages);

            string content =
                $"Wybrane filtry: {result.SelectedFiltersCount}{Environment.NewLine}" +
                $"Widoki docelowe: {result.ProcessedViewsCount}{Environment.NewLine}" +
                $"Dodane: {result.AddedFiltersCount}{Environment.NewLine}" +
                $"Zaktualizowane: {result.UpdatedFiltersCount}{Environment.NewLine}" +
                $"Pominięte: {result.SkippedFiltersCount}";

            if (!string.IsNullOrWhiteSpace(details))
            {
                content += $"{Environment.NewLine}{Environment.NewLine}Szczegóły:{Environment.NewLine}{details}";

                if (result.Messages.Count > 12)
                {
                    content += $"{Environment.NewLine}...";
                }
            }

            TaskDialog.Show("Kopiowanie filtrów", content);
        }

        private void SourceSearchTextBox_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplySourceFilters();
        }

        private void DestinationSearchTextBox_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplyDestinationFilters();
        }

        private void SourceFilters_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            ApplySourceFilters();
        }

        private void DestinationFilters_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            ApplyDestinationFilters();
        }

        private List<ViewListItem> FilterViews(string searchText, string? viewType, string? itemType)
        {
            const string allValue = "__ALL__";
            IEnumerable<ViewListItem> query = _allViews;

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(item => item.DisplayName.IndexOf(searchText.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0);
            }

            if (!string.IsNullOrWhiteSpace(viewType) && !string.Equals(viewType, allValue, StringComparison.Ordinal))
            {
                query = query.Where(item => string.Equals(item.ViewTypeName, viewType, StringComparison.CurrentCultureIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(itemType) && !string.Equals(itemType, allValue, StringComparison.Ordinal))
            {
                query = query.Where(item => string.Equals(item.ItemTypeName, itemType, StringComparison.CurrentCultureIgnoreCase));
            }

            return query.ToList();
        }

        private void RemoveSourceFromDestinations(ElementId sourceId)
        {
            ApplyDestinationFilters();
        }

        private void SelectAllFiltersButton_OnClick(object sender, RoutedEventArgs e)
        {
            FiltersListBox.SelectAll();
        }

        private void ClearFiltersButton_OnClick(object sender, RoutedEventArgs e)
        {
            FiltersListBox.UnselectAll();
        }

        private void SelectAllDestinationsButton_OnClick(object sender, RoutedEventArgs e)
        {
            DestinationViewsListBox.SelectAll();
        }

        private void ClearDestinationsButton_OnClick(object sender, RoutedEventArgs e)
        {
            DestinationViewsListBox.UnselectAll();
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SetStatus(string message)
        {
            FooterBar.StatusText = message;
        }

        private void InitializeViewFilters()
        {
            const string allValue = "__ALL__";

            List<SelectionListItem> viewTypeOptions =
            [
                new SelectionListItem(allValue, "Wszystkie")
            ];

            viewTypeOptions.AddRange(_allViews
                .Select(item => item.ViewTypeName)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                .Select(name => new SelectionListItem(name, name)));

            List<SelectionListItem> typeOptions =
            [
                new SelectionListItem(allValue, "Wszystkie"),
                new SelectionListItem("View", "View"),
                new SelectionListItem("Template", "Template")
            ];

            SourceViewTypeComboBox.ItemsSource = viewTypeOptions;
            SourceViewTypeComboBox.SelectedValue = allValue;
            DestinationViewTypeComboBox.ItemsSource = viewTypeOptions.ToList();
            DestinationViewTypeComboBox.SelectedValue = allValue;

            SourceTypeComboBox.ItemsSource = typeOptions;
            SourceTypeComboBox.SelectedValue = allValue;
            DestinationTypeComboBox.ItemsSource = typeOptions.ToList();
            DestinationTypeComboBox.SelectedValue = allValue;
        }

        private void ApplySourceFilters()
        {
            SourceViewsListBox.ItemsSource = FilterViews(
                SourceSearchTextBox.Text,
                SourceViewTypeComboBox.SelectedValue as string,
                SourceTypeComboBox.SelectedValue as string);
        }

        private void ApplyDestinationFilters()
        {
            IEnumerable<ViewListItem> filtered = FilterViews(
                DestinationSearchTextBox.Text,
                DestinationViewTypeComboBox.SelectedValue as string,
                DestinationTypeComboBox.SelectedValue as string);

            ViewListItem? selectedSource = SourceViewsListBox.SelectedItem as ViewListItem;
            if (selectedSource != null)
            {
                filtered = filtered.Where(item => item.Id.Value != selectedSource.Id.Value);
            }

            DestinationViewsListBox.ItemsSource = filtered.ToList();
        }
    }
}
