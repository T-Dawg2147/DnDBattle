using DnDBattle.Services;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace DnDBattle.Views
{
    /// <summary>
    /// Interaction logic for DatabaseImportWindow.xaml
    /// </summary>
    public partial class DatabaseImportWindow : Window
    {
        private CreatureDatabaseService _dbService;
        private string _selectedFolder;

        public DatabaseImportWindow()
        {
            InitializeComponent();
            _dbService = new CreatureDatabaseService();

            UpdateStatus();
        }

        private async void UpdateStatus()
        {
            var count = await _dbService.GetCreatureCountAsync();
            TxtStatus.Text = $"Database:  {count} creatures";
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select any JSON file in the creature folder",
                Filter = "JSON filed (*.json)|*.json|All files (*.*)|*.*",
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

            BtnImport.IsEnabled = false;
            BtnBrowse.IsEnabled = false;
            ProgressBar.Value = 0;

            try
            {
                var jsonFiles = Directory.GetFiles(_selectedFolder, "*.json");
                int totalFiles = jsonFiles.Length;
                int totalImported = 0;

                for (int i = 0; i < jsonFiles.Length; i++)
                {
                    var file = jsonFiles[i];
                    var fileName = Path.GetFileName(file);

                    LogMessage($"Importing {fileName}...");

                    try
                    {
                        var count = await _dbService.ImportFromJsonFileAsync(file);
                        totalImported += count;
                        LogMessage($"  ✓ Imported {count} creatures from {fileName}");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"  ✗ Error importing {fileName}: {ex.Message}");
                    }

                    ProgressBar.Value = ((i + 1) * 100) / totalFiles;
                }

                LogMessage($"\n=== Import Complete ===");
                LogMessage($"Total creatures imported: {totalImported}");

                UpdateStatus();

                MessageBox.Show($"Successfully imported {totalImported} creatures! ",
                    "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"Error during import: {ex.Message}");
                MessageBox.Show($"Import failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnImport.IsEnabled = true;
                BtnBrowse.IsEnabled = true;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LogMessage(string message)
        {
            TxtLog.AppendText(message + Environment.NewLine);
            TxtLog.ScrollToEnd();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _dbService?.Dispose();
        }
    }
}
