using DnDBattle.Core.Models;

namespace DnDBattle.Core.Interfaces;

public interface IPersistenceService
{
    Task SaveEncounterAsync(EncounterSnapshot snapshot, string filePath, CancellationToken ct = default);
    Task<EncounterSnapshot?> LoadEncounterAsync(string filePath, CancellationToken ct = default);
    Task AutosaveAsync(EncounterSnapshot snapshot, CancellationToken ct = default);
}
