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
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace RUKNBIM.ElementNavigator
{
    public partial class ElementNavigatorViewModel : ObservableObject
    {
        private const string RepoOwner = "engahmedkhalaf";
        private const string RepoName = "RUKNBIM.ElementNavigator";
        private const string RepoUrl = "https://github.com/" + RepoOwner + "/" + RepoName;
        private const string LatestReleaseApi = "https://api.github.com/repos/" + RepoOwner + "/" + RepoName + "/releases/latest";

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
        [ObservableProperty] private string _updateStatus = "Checking for updates...";
        [ObservableProperty] private Brush _updateStatusBrush = Brushes.Gray;

        public ObservableCollection<string> FoundIds { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> MissingIds { get; } = new ObservableCollection<string>();

        public ElementNavigatorViewModel()
        {
            IsolateSelected = true;
            ZoomToSelected = true;
            _searchEngine = new NavisworksSearchEngine();
            _dataService = new ExcelDataService();
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
            RefreshModelInfo();
            _ = CheckForUpdatesAsync();
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

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) })
                {
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("RUKNBIM-ElementNavigator");
                    http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                    var json = await http.GetStringAsync(LatestReleaseApi);

                    var match = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                    if (!match.Success)
                    {
                        SetUpdateStatus("Update check unavailable", Brushes.Gray);
                        return;
                    }

                    var tag = match.Groups[1].Value;
                    var latest = ParseVersion(tag);
                    var current = ParseVersion(Version);

                    if (latest > current)
                        SetUpdateStatus($"Update available: {tag}", Brushes.OrangeRed);
                    else
                        SetUpdateStatus("Plugin is up to date", Brushes.LimeGreen);
                }
            }
            catch
            {
                SetUpdateStatus("Could not check for updates", Brushes.Gray);
            }
        }

        private void SetUpdateStatus(string text, Brush brush)
        {
            if (System.Windows.Application.Current?.Dispatcher is { } d && !d.CheckAccess())
                d.Invoke(() => { UpdateStatus = text; UpdateStatusBrush = brush; });
            else
            {
                UpdateStatus = text;
                UpdateStatusBrush = brush;
            }
        }

        private static Version ParseVersion(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new Version(0, 0, 0, 0);
            var cleaned = Regex.Replace(s, "[^0-9.]", "");
            return System.Version.TryParse(cleaned, out var v) ? v : new Version(0, 0, 0, 0);
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
                $"RUKNBIM Element Navigator\nVersion {Version}\n\nA Navisworks addin for fast Revit Element ID search.\n\nPrepared by: Ahmed Khalaf (BIM Manager)\nMobile: +966542554127\nEmail: engkhalaf7@gmail.com",
                "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void Help() => OpenUrl($"{RepoUrl}#readme");

        [RelayCommand]
        private void Update() => OpenUrl("https://github.com/engahmedkhalaf/RUKNBIM_ElementNavigator_Setup_1.0.0");

        private static void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { /* swallow */ }
        }
    }
}
