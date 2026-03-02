using DnDBattle.Core.Models;

namespace DnDBattle.Core.Interfaces;

public interface ICreatureRepository
{
    Task<IReadOnlyList<CreatureRecord>> GetAllAsync(CancellationToken ct = default);
    Task<CreatureRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(CreatureRecord creature, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
