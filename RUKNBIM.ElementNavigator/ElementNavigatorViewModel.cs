using Autodesk.Navisworks.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RUKNBIM.ElementNavigator
{
    public partial class ElementNavigatorViewModel : ObservableObject
    {
        private readonly NavisworksSearchEngine _searchEngine;
        private readonly IDataService _dataService;

        [ObservableProperty]
        private string _elementIdsInput;

        [ObservableProperty]
        private bool _isolateSelected;

        [ObservableProperty]
        private bool _zoomToSelected;

        private List<string> _lastFoundIds = new List<string>();
        private List<string> _lastMissingIds = new List<string>();

        public ElementNavigatorViewModel()
        {
            IsolateSelected = true;
            ZoomToSelected = true;
            _searchEngine = new NavisworksSearchEngine();
            _dataService = new ExcelDataService();
        }

        [RelayCommand]
        private void Search()
        {
            if (string.IsNullOrWhiteSpace(ElementIdsInput))
            {
                MessageBox.Show("Please enter Element IDs to search.", "Element Navigator", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ids = ElementIdsInput.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(id => id.Trim())
                                     .Where(id => !string.IsNullOrEmpty(id))
                                     .Distinct()
                                     .ToList();

            if (!ids.Any()) return;

            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            
            // Build cache for performance
            _searchEngine.BuildCache(doc);

            // Find elements
            var items = _searchEngine.FindElements(ids);
            
            // Highlight and perform actions
            NavisworksActions.HighlightAndSelect(doc, items, IsolateSelected, ZoomToSelected);

            // Track found vs missing for reporting
            _lastMissingIds = _searchEngine.GetMissingIds(ids);
            _lastFoundIds = ids.Except(_lastMissingIds).ToList();

            int foundCount = _lastFoundIds.Count;
            int missingCount = _lastMissingIds.Count;

            MessageBox.Show($"Search Complete.\nFound: {foundCount}\nMissing: {missingCount}", "Element Navigator", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void ImportExcel()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls|CSV Files|*.csv",
                Title = "Select Excel File with Element IDs"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var ids = _dataService.ImportIdsFromExcel(openFileDialog.FileName);
                    ElementIdsInput = string.Join(Environment.NewLine, ids);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading Excel file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void ExportReport()
        {
            if (!_lastFoundIds.Any() && !_lastMissingIds.Any())
            {
                MessageBox.Show("No search has been performed yet to generate a report.", "Export Report", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Save Element Report",
                FileName = "ElementReport.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    _dataService.ExportReportToExcel(saveFileDialog.FileName, _lastFoundIds, _lastMissingIds);
                    MessageBox.Show("Report exported successfully.", "Export Report", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void ResetModel()
        {
            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            if (doc != null)
            {
                NavisworksActions.RestoreFullModel(doc);
            }
        }
    }
}
