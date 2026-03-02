namespace DnDBattle.App.Configuration;

public sealed class LightingSettings
{
    public bool EnableDynamicLighting { get; set; } = false;
    public bool EnableFogOfWar { get; set; } = false;
    public double AmbientLightLevel { get; set; } = 1.0; // 0=dark, 1=bright daylight
    public bool EnableShadowCasting { get; set; } = false;
    public bool EnableDarkvision { get; set; } = true;
}
