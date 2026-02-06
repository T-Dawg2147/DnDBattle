using DnDBattle.Services;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DnDBattle.Views;
using DnDBattle.Views.Combat;
using DnDBattle.Views.Creatures;
using DnDBattle.Views.Dice;
using DnDBattle.Views.Editors;
using DnDBattle.Views.Effects;
using DnDBattle.Views.Features;
using DnDBattle.Views.Multiplayer;
using DnDBattle.Views.Settings;
using DnDBattle.Views.Spells;
using DnDBattle.Views.TileMap;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.Persistence;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;

namespace DnDBattle.Views.Encounters
{
    public partial class DatabaseImportWindow : Window
    {
        private CreatureDatabaseService _dbService;
        private string _selectedFolder;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isImporting = false;

        public DatabaseImportWindow()
        {
            InitializeComponent();
            _dbService = new CreatureDatabaseService();
            UpdateStatus();
        }

        private async void UpdateStatus()
        {
            try
            {
                var count = await Task.Run(() => _dbService.GetCreatureCountAsync());
                TxtStatus.Text = $"Database:  {count} creatures";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Database: Error - {ex.Message}";
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select any JSON file in the creature folder",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedFolder = Path.GetDirectoryName(dialog.FileName);
                TxtFolderPath.Text = _selectedFolder;

                var jsonFiles = Directory.GetFiles(_selectedFolder, "*.json");
                LogMessage($"Found {jsonFiles.Length} JSON files in folder.");

                foreach (var file in jsonFiles)
                {
                    LogMessage($"  - {Path.GetFileName(file)}");
                }

                BtnImport.IsEnabled = jsonFiles.Length > 0;
            }
        }

        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFolder))
            {
                MessageBox.Show("Please select a folder first.", "No Folder",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_isImporting)
            {
                // Cancel the current import
                _cancellationTokenSource?.Cancel();
                return;
            }

            _isImporting = true;
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            BtnImport.Content = "Cancel";
            BtnBrowse.IsEnabled = false;
            ProgressBar.Value = 0;

            int totalImported = 0;
            int totalErrors = 0;
            int totalSkipped = 0;

            try
            {
                var jsonFiles = Directory.GetFiles(_selectedFolder, "*.json");
                int totalFiles = jsonFiles.Length;

                for (int i = 0; i < jsonFiles.Length; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LogMessage("\n⚠ Import cancelled by user.");
                        break;
                    }

                    var file = jsonFiles[i];
                    var fileName = Path.GetFileName(file);

                    LogMessage($"Importing {fileName}...");

                    try
                    {
                        // Run the import on a background thread
                        var result = await Task.Run(async () =>
                        {
                            return await ImportFileWithDetailsAsync(file, cancellationToken);
                        }, cancellationToken);

                        totalImported += result.Imported;
                        totalErrors += result.Errors;
                        totalSkipped += result.Skipped;

                        // Update UI on main thread
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (result.Imported > 0)
                                LogMessage($"  ✓ Imported {result.Imported} creatures");
                            if (result.Skipped > 0)
                                LogMessage($"  ⊘ Skipped {result.Skipped} duplicates");
                            if (result.Errors > 0)
                                LogMessage($"  ✗ {result.Errors} errors");

                            ProgressBar.Value = ((i + 1) * 100) / totalFiles;
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        LogMessage($"  ⚠ Cancelled");
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"  ✗ Error:  {ex.Message}");
                        totalErrors++;
                    }

                    // Small delay to keep UI responsive
                    await Task.Delay(10, CancellationToken.None);
                }

                LogMessage($"\n{'=',-40}");
                LogMessage($"=== Import Complete ===");
                LogMessage($"Total imported: {totalImported}");
                LogMessage($"Total skipped (duplicates): {totalSkipped}");
                LogMessage($"Total errors: {totalErrors}");
                LogMessage($"{'=',-40}");

                UpdateStatus();

                if (!cancellationToken.IsCancellationRequested)
                {
                    MessageBox.Show(
                        $"Import Complete!\n\n" +
                        $"Imported:  {totalImported}\n" +
                        $"Skipped (duplicates): {totalSkipped}\n" +
                        $"Errors: {totalErrors}",
                        "Import Complete",
                        MessageBoxButton.OK,
                        totalErrors > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error during import: {ex.Message}");
                MessageBox.Show($"Import failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isImporting = false;
                BtnImport.Content = "Import All";
                BtnImport.IsEnabled = true;
                BtnBrowse.IsEnabled = true;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async Task<ImportResult> ImportFileWithDetailsAsync(string filePath, CancellationToken cancellationToken)
        {
            var result = new ImportResult();

            if (!File.Exists(filePath))
                return result;

            var category = Path.GetFileNameWithoutExtension(filePath);

            try
            {
                string json;
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    json = await reader.ReadToEndAsync();
                }

                // Remove BOM if present
                if (json.Length > 0 && json[0] == '\uFEFF')
                    json = json.Substring(1);

                json = json.Trim();

                if (string.IsNullOrEmpty(json))
                    return result;

                using (var document = System.Text.Json.JsonDocument.Parse(json))
                {
                    var root = document.RootElement;

                    if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var creatureElement in root.EnumerateArray())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            try
                            {
                                var addResult = await _dbService.AddCreatureFromJsonElementAsync(
                                    creatureElement, category, filePath);

                                switch (addResult)
                                {
                                    case AddCreatureResult.Added:
                                        result.Imported++;
                                        break;
                                    case AddCreatureResult.Skipped:
                                        result.Skipped++;
                                        break;
                                    case AddCreatureResult.Error:
                                        result.Errors++;
                                        break;
                                }
                            }
                            catch (Exception)
                            {
                                result.Errors++;
                            }
                        }
                    }
                    else if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        var addResult = await _dbService.AddCreatureFromJsonElementAsync(
                            root, category, filePath);

                        switch (addResult)
                        {
                            case AddCreatureResult.Added:
                                result.Imported++;
                                break;
                            case AddCreatureResult.Skipped:
                                result.Skipped++;
                                break;
                            case AddCreatureResult.Error:
                                result.Errors++;
                                break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                result.Errors++;
            }

            return result;
        }

        private async void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to delete ALL creatures fomr the database?\n\n" +
                "This cannot be undone! ",
                "Clear Database",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                BtnClear.IsEnabled = false;
                BtnImport.IsEnabled = false;

                LogMessage("Clearing database...");

                await Task.Run(async () =>
                {
                    await _dbService.ClearAllCreaturesAsync();
                });

                LogMessage("✓ Database cleared successfully.");
                UpdateStatus();
            }
            catch (Exception ex)
            {
                LogMessage($"✗ Error clearing database: {ex.Message}");
                MessageBox.Show($"Failed to clear database: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnClear.IsEnabled = true;
                BtnImport.IsEnabled = !string.IsNullOrEmpty(_selectedFolder);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_isImporting)
            {
                var result = MessageBox.Show(
                    "Import is in progress. Cancel and close? ",
                    "Import Running",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _cancellationTokenSource?.Cancel();
                    Close();
                }
            }
            else
            {
                Close();
            }
        }

        private void LogMessage(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => LogMessage(message));
                return;
            }

            TxtLog.AppendText(message + Environment.NewLine);
            TxtLog.ScrollToEnd();
        }

        protected override void OnClosed(EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            base.OnClosed(e);
            _dbService?.Dispose();
        }

        private class ImportResult
        {
            public int Imported { get; set; }
            public int Skipped { get; set; }
            public int Errors { get; set; }
        }
    }
}