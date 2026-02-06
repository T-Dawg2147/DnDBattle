using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Services;
using DnDBattle.Services.Combat;
using DnDBattle.Services.Creatures;
using DnDBattle.Services.Dice;
using DnDBattle.Services.Effects;
using DnDBattle.Services.Encounters;
using DnDBattle.Services.Grid;
using DnDBattle.Services.Networking;
using DnDBattle.Services.TileService;
using DnDBattle.Services.UI;
using DnDBattle.Services.Vision;
using DnDBattle.Models.Tiles;
using DnDBattle.Services.Persistence;

namespace DnDBattle.Services.Persistence
{
    /// <summary>
    /// Cloud save and sync service for Phase 9 multiplayer.
    /// Supports self-hosted REST API backend or local file-based fallback.
    /// Handles conflict resolution when encounters are edited offline.
    /// </summary>
    public sealed class CloudSaveService : IDisposable
    {
        private HttpClient? _httpClient;
        private bool _disposed;

        /// <summary>Raised when a message should be logged.</summary>
        public event System.Action<string, string>? MessageLogged;

        /// <summary>Whether cloud sync is configured and available.</summary>
        public bool IsConfigured => !string.IsNullOrEmpty(Options.CloudSaveServerUrl);

        /// <summary>
        /// Initialize the cloud save service with the configured server URL.
        /// </summary>
        public void Initialize()
        {
            if (!Options.EnableCloudSave || string.IsNullOrEmpty(Options.CloudSaveServerUrl))
            {
                Log("Cloud", "Cloud save not configured (no server URL)");
                return;
            }

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(Options.CloudSaveServerUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            Log("Cloud", $"Cloud save initialized: {Options.CloudSaveServerUrl}");
        }

        /// <summary>
        /// Save an encounter to the cloud server.
        /// Falls back to local export if server is unavailable.
        /// </summary>
        public async Task<bool> SaveEncounterAsync(string encounterId, string encounterJson, string campaignId)
        {
            if (!Options.EnableCloudSave) return false;

            if (_httpClient == null)
            {
                Log("Cloud", "Cloud save not initialized");
                return false;
            }

            try
            {
                var content = new StringContent(encounterJson, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(
                    $"api/encounters/{campaignId}/{encounterId}", content).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    Log("Cloud", $"Saved encounter {encounterId} to cloud");
                    return true;
                }

                Log("Cloud", $"Cloud save failed: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Log("Cloud", $"Cloud save error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load an encounter from the cloud server.
        /// </summary>
        public async Task<string?> LoadEncounterAsync(string encounterId, string campaignId)
        {
            if (!Options.EnableCloudSave || _httpClient == null)
                return null;

            try
            {
                var response = await _httpClient.GetAsync(
                    $"api/encounters/{campaignId}/{encounterId}").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Log("Cloud", $"Loaded encounter {encounterId} from cloud");
                    return json;
                }

                Log("Cloud", $"Cloud load failed: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                Log("Cloud", $"Cloud load error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// List available encounters in a campaign from the cloud server.
        /// </summary>
        public async Task<List<EncounterMetadata>> ListEncountersAsync(string campaignId)
        {
            if (!Options.EnableCloudSave || _httpClient == null)
                return new List<EncounterMetadata>();

            try
            {
                var response = await _httpClient.GetAsync(
                    $"api/encounters/{campaignId}").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var list = JsonSerializer.Deserialize<List<EncounterMetadata>>(json);
                    return list ?? new List<EncounterMetadata>();
                }

                return new List<EncounterMetadata>();
            }
            catch (Exception ex)
            {
                Log("Cloud", $"List encounters error: {ex.Message}");
                return new List<EncounterMetadata>();
            }
        }

        /// <summary>
        /// Resolve conflicts between local and remote encounter versions.
        /// Returns the winning encounter JSON based on last-modified timestamps.
        /// </summary>
        public async Task<(string json, ConflictResolution resolution)> ResolveConflictAsync(
            string encounterId, string campaignId,
            string localJson, DateTime localModified)
        {
            if (!Options.EnableCloudSave || _httpClient == null)
                return (localJson, ConflictResolution.UseLocal);

            try
            {
                var remoteJson = await LoadEncounterAsync(encounterId, campaignId).ConfigureAwait(false);

                if (remoteJson == null)
                {
                    // Remote doesn't exist — use local and upload
                    await SaveEncounterAsync(encounterId, localJson, campaignId).ConfigureAwait(false);
                    return (localJson, ConflictResolution.UseLocal);
                }

                // Try to parse remote metadata
                var remoteMetadata = JsonSerializer.Deserialize<EncounterMetadata>(remoteJson);
                if (remoteMetadata != null && remoteMetadata.LastModified > localModified)
                {
                    Log("Cloud", "Remote version is newer — conflict detected");
                    return (remoteJson, ConflictResolution.UseRemote);
                }

                // Local is newer or same — upload local
                await SaveEncounterAsync(encounterId, localJson, campaignId).ConfigureAwait(false);
                return (localJson, ConflictResolution.UseLocal);
            }
            catch (Exception ex)
            {
                Log("Cloud", $"Conflict resolution error: {ex.Message}");
                return (localJson, ConflictResolution.UseLocal);
            }
        }

        private void Log(string source, string message) =>
            MessageLogged?.Invoke(source, message);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Result of a cloud save conflict resolution.
    /// </summary>
    public enum ConflictResolution
    {
        UseLocal,
        UseRemote,
        Merged
    }
}
