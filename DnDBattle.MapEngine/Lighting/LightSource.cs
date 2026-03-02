using System.Windows;

namespace DnDBattle.MapEngine.Lighting;

public sealed class LightSource
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Point Position { get; set; }
    public double BrightRadiusFeet { get; set; }
    public double DimRadiusFeet { get; set; }
    public System.Windows.Media.Color Color { get; set; } = System.Windows.Media.Colors.LightYellow;
    public bool IsActive { get; set; } = true;
    public Guid? OwnerId { get; set; } // Null = environmental
}
