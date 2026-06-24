using Autodesk.Navisworks.Api;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace RUKNBIM.ElementNavigator
{
    public partial class ElementNavigatorViewModel : ObservableObject
    {
        private const string RepoUrl = "https://github.com/engahmedkhalaf/RUKNBIM.ElementNavigator";

        private readonly NavisworksSearchEngine _searchEngine;
        private readonly IDataService _dataService;

        [ObservableProperty] private string _elementIdsInput;
        [ObservableProperty] private bool _isolateSelected;
        [ObservableProperty] private bool _zoomToSelected;
        [ObservableProperty] private string _statusMessage = "Ready to search Revit Element IDs";
        [ObservableProperty] private int _foundCount;
        [ObservableProperty] private int _missingCount;
        [ObservableProperty] private int _selectedCount;
        [ObservableProperty] private string _version;
        [ObservableProperty] private string _modelName = "(no model loaded)";

        public ObservableCollection<string> FoundIds { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> MissingIds { get; } = new ObservableCollection<string>();

        public ElementNavigatorViewModel()
        {
            IsolateSelected = true;
            ZoomToSelected = true;
            _searchEngine = new NavisworksSearchEngine();
            _dataService = new ExcelDataService();
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
            RefreshModelInfo();
        }

        private void RefreshModelInfo()
        {
            try
            {
                var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
                if (doc == null) return;
                ModelName = string.IsNullOrEmpty(doc.CurrentFileName)
                    ? "(unsaved)"
                    : Path.GetFileName(doc.CurrentFileName);
                SelectedCount = doc.CurrentSelection?.SelectedItems?.Count ?? 0;
            }
            catch { /* Navisworks may not be ready yet */ }
        }

        [RelayCommand]
        private void Search()
        {
            if (string.IsNullOrWhiteSpace(ElementIdsInput))
            {
                StatusMessage = "Please enter Element IDs to search.";
                return;
            }

            var ids = ElementIdsInput.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(id => id.Trim())
                                     .Where(id => !string.IsNullOrEmpty(id))
                                     .Distinct()
                                     .ToList();

            if (!ids.Any()) return;

            StatusMessage = $"Searching {ids.Count} IDs...";
            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;

            _searchEngine.BuildCache(doc);
            var items = _searchEngine.FindElements(ids);
            NavisworksActions.HighlightAndSelect(doc, items, IsolateSelected, ZoomToSelected);

            var missing = _searchEngine.GetMissingIds(ids);
            var found = ids.Except(missing).ToList();

            FoundIds.Clear();
            foreach (var id in found) FoundIds.Add(id);
            MissingIds.Clear();
            foreach (var id in missing) MissingIds.Add(id);

            FoundCount = found.Count;
            MissingCount = missing.Count;
            RefreshModelInfo();

            StatusMessage = $"Completed. Found {FoundCount} of {ids.Count} elements.";
        }

        [RelayCommand]
        private void RestoreFullModel()
        {
            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            if (doc == null) return;
            NavisworksActions.RestoreFullModel(doc);
            StatusMessage = "Full model restored.";
            RefreshModelInfo();
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
                    StatusMessage = $"Imported {ids.Count()} IDs from Excel.";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Import failed: {ex.Message}";
                    MessageBox.Show($"Error reading Excel file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void ExportReport()
        {
            if (FoundIds.Count == 0 && MissingIds.Count == 0)
            {
                StatusMessage = "Nothing to export — run a search first.";
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
                    _dataService.ExportReportToExcel(saveFileDialog.FileName, FoundIds.ToList(), MissingIds.ToList());
                    StatusMessage = "Report exported successfully.";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Export failed: {ex.Message}";
                    MessageBox.Show($"Error exporting report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void About()
        {
            MessageBox.Show(
                $"RUKNBIM Element Navigator\nVersion {Version}\n\nA Navisworks addin for fast Revit Element ID search.\n\n© RUKNBIM",
                "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void Help() => OpenUrl($"{RepoUrl}#readme");

        [RelayCommand]
        private void Update() => OpenUrl($"{RepoUrl}/releases");

        private static void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { /* swallow */ }
        }
    }
}
