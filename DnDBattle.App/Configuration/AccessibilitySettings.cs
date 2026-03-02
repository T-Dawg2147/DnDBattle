namespace DnDBattle.App.Configuration;

public sealed class AccessibilitySettings
{
    public bool HighContrastMode { get; set; } = false;
    public double FontSizeMultiplier { get; set; } = 1.0;
    public bool EnableScreenReader { get; set; } = false;
    public bool ShowTokenLabels { get; set; } = true;
    public bool ColorBlindFriendly { get; set; } = false;
}
