using Microsoft.Maui.Graphics;

namespace MusicPlayer.Services;

/// <summary>
/// Semantic color palette for the light and dark themes. The values are pushed
/// into <see cref="Microsoft.Maui.Controls.Application.Resources"/> at runtime so
/// that every <c>DynamicResource</c> usage refreshes as soon as the theme changes.
/// </summary>
internal static class ThemePalette
{
    // Light theme built from the primary green (#00B64F family) plus the
    // orange (secondary A) and pink (secondary B) accents.
    public static readonly IReadOnlyDictionary<string, Color> Light = new Dictionary<string, Color>
    {
        ["PageBackground"] = Color.FromArgb("#F2F7F3"),
        ["Surface"] = Color.FromArgb("#FFFFFF"),
        ["SurfaceAlt"] = Color.FromArgb("#E6F0EA"),
        ["BorderColor"] = Color.FromArgb("#D2E2D8"),
        ["TextPrimary"] = Color.FromArgb("#0F2419"),
        ["TextSecondary"] = Color.FromArgb("#4C6459"),
        ["TextDisabled"] = Color.FromArgb("#8AA396"),
        ["TabInactive"] = Color.FromArgb("#6B8377"),
        ["Accent"] = Color.FromArgb("#00B64F"),
        ["AccentStrong"] = Color.FromArgb("#007633"),
        ["AccentLight"] = Color.FromArgb("#62DA97"),
        ["OnAccent"] = Color.FromArgb("#FFFFFF"),
        ["SecondaryA"] = Color.FromArgb("#FF8900"),
        ["SecondaryALight"] = Color.FromArgb("#FFBE73"),
        ["SecondaryB"] = Color.FromArgb("#DC0055"),
        ["SecondaryBLight"] = Color.FromArgb("#EE6B9E"),
        ["Error"] = Color.FromArgb("#C0392B")
    };

    // Dark theme from the provided design tokens (dark green-graphite base,
    // light green accent, softer secondary shades for text/icons).
    public static readonly IReadOnlyDictionary<string, Color> Dark = new Dictionary<string, Color>
    {
        ["PageBackground"] = Color.FromArgb("#0B0F0D"),
        ["Surface"] = Color.FromArgb("#17221C"),
        ["SurfaceAlt"] = Color.FromArgb("#1F2E26"),
        ["BorderColor"] = Color.FromArgb("#2A3D33"),
        ["TextPrimary"] = Color.FromArgb("#EAF7EF"),
        ["TextSecondary"] = Color.FromArgb("#A9C0B3"),
        ["TextDisabled"] = Color.FromArgb("#6F8579"),
        ["TabInactive"] = Color.FromArgb("#A9C0B3"),
        ["Accent"] = Color.FromArgb("#37DA7E"),
        ["AccentStrong"] = Color.FromArgb("#00B64F"),
        ["AccentLight"] = Color.FromArgb("#62DA97"),
        ["OnAccent"] = Color.FromArgb("#06140C"),
        ["SecondaryA"] = Color.FromArgb("#FFA640"),
        ["SecondaryALight"] = Color.FromArgb("#FFBE73"),
        ["SecondaryB"] = Color.FromArgb("#EE3B80"),
        ["SecondaryBLight"] = Color.FromArgb("#EE6B9E"),
        ["Error"] = Color.FromArgb("#EE6B6B")
    };
}
