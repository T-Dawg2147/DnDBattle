namespace DnDBattle.Data.Entities;

public sealed class EncounterEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SavedAt { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
}
