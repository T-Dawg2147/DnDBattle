using System.Windows;

namespace DnDBattle.MapEngine.Lighting;

public sealed class LightingService
{
    private readonly List<LightSource> _sources = new();
    private double _cellSizePx = 50.0;

    public IReadOnlyList<LightSource> Sources => _sources;
    public double AmbientLight { get; set; } = 0.0; // 0 = pitch black, 1 = full daylight

    public void Configure(double cellSizePx) => _cellSizePx = cellSizePx;

    public void AddSource(LightSource source) => _sources.Add(source);
    public void RemoveSource(Guid id) => _sources.RemoveAll(s => s.Id == id);

    /// <summary>Returns the light level at a world position (0.0 = dark, 1.0 = bright).</summary>
    public double GetLightLevel(Point worldPos)
    {
        double max = AmbientLight;
        foreach (var src in _sources.Where(s => s.IsActive))
        {
            double distPx = (worldPos - src.Position).Length;
            double distFeet = distPx / _cellSizePx * 5.0;
            double level = distFeet <= src.BrightRadiusFeet ? 1.0
                : distFeet <= src.DimRadiusFeet ? 0.5
                : 0.0;
            max = Math.Max(max, level);
        }
        return max;
    }

    public bool IsInBrightLight(Point worldPos) => GetLightLevel(worldPos) >= 1.0;
    public bool IsInDimLight(Point worldPos) => GetLightLevel(worldPos) is >= 0.5 and < 1.0;
    public bool IsInDarkness(Point worldPos) => GetLightLevel(worldPos) < 0.5;
}
