using System;
using System.Windows;
using DnDBattle.Models;
using DnDBattle.Services;

namespace DnDBattle.Views
{
    public partial class CloudSaveWindow : Window
    {
        private readonly CloudSaveService _cloudSave = new();

        public CloudSaveWindow()
        {
            InitializeComponent();
            ServerUrlBox.Text = Options.CloudSaveServerUrl;

            _cloudSave.MessageLogged += OnMessageLogged;
            _cloudSave.Initialize();
        }

        private async void SaveEncounter_Click(object sender, RoutedEventArgs e)
        {
            var campaignId = CampaignIdBox.Text?.Trim() ?? "default";
            var encounterId = Guid.NewGuid().ToString("N");

            StatusText.Text = "Saving...";
            // Placeholder – a real integration would serialize the encounter from the main view model.
            var encounterJson = System.Text.Json.JsonSerializer.Serialize(new { Id = encounterId, CampaignId = campaignId, SavedAt = DateTime.UtcNow });
            var success = await _cloudSave.SaveEncounterAsync(encounterId, encounterJson, campaignId);
            StatusText.Text = success ? $"Saved encounter {encounterId}" : "Save failed";
        }

        private async void RefreshList_Click(object sender, RoutedEventArgs e)
        {
            var campaignId = CampaignIdBox.Text?.Trim() ?? "default";
            StatusText.Text = "Refreshing...";

            var encounters = await _cloudSave.ListEncountersAsync(campaignId);
            EncounterList.ItemsSource = encounters;
            StatusText.Text = $"Found {encounters.Count} encounter(s)";
        }

        private async void LoadEncounter_Click(object sender, RoutedEventArgs e)
        {
            if (EncounterList.SelectedItem is not EncounterMetadata meta)
            {
                StatusText.Text = "Select an encounter first";
                return;
            }

            var campaignId = CampaignIdBox.Text?.Trim() ?? "default";
            StatusText.Text = "Loading...";

            var json = await _cloudSave.LoadEncounterAsync(meta.Id, campaignId);
            StatusText.Text = json != null ? $"Loaded encounter {meta.Id}" : "Load failed";
        }

        private void OnMessageLogged(string source, string message)
        {
            Dispatcher.Invoke(() => StatusText.Text = $"[{source}] {message}");
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _cloudSave.Dispose();
        }
    }
}
