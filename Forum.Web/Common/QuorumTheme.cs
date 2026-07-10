// Forum.Web/Common/QuorumTheme.cs
using MudBlazor;

namespace Forum.Web.Common;

/// <summary>
/// Forum-wide MudBlazor theme. Dark-only; hues echo the landing page's locked
/// palette (bg #07070c, accent #4a6ee0, violet #7a3be0, Space Grotesk/Manrope)
/// so the app and the marketing page read as one product.
/// </summary>
public static class QuorumTheme
{
    public static readonly MudTheme Theme = new()
    {
        PaletteDark = new PaletteDark
        {
            Black = "#050509",
            Background = "#07070c",
            BackgroundGray = "#0a0a12",
            Surface = "#12121c",
            AppbarBackground = "#0a0a12",
            AppbarText = "#f1f1f8",
            DrawerBackground = "#0a0a12",
            DrawerText = "#a6a7ba",
            DrawerIcon = "#a6a7ba",
            Primary = "#4a6ee0",
            Secondary = "#7a3be0",
            Tertiary = "#7a8ff0",
            Info = "#4a6ee0",
            Success = "#2ea87a",
            Warning = "#d99a2b",
            Error = "#e05252",
            TextPrimary = "#f1f1f8",
            TextSecondary = "#a6a7ba",
            TextDisabled = "rgba(241,241,248,0.38)",
            ActionDefault = "#a6a7ba",
            ActionDisabled = "rgba(241,241,248,0.26)",
            ActionDisabledBackground = "rgba(241,241,248,0.10)",
            LinesDefault = "rgba(255,255,255,0.10)",
            LinesInputs = "rgba(255,255,255,0.18)",
            Divider = "rgba(255,255,255,0.10)",
            TableLines = "rgba(255,255,255,0.10)",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Manrope", "system-ui", "sans-serif"],
            },
            H1 = new H1Typography { FontFamily = ["Space Grotesk", "Manrope", "sans-serif"] },
            H2 = new H2Typography { FontFamily = ["Space Grotesk", "Manrope", "sans-serif"] },
            H3 = new H3Typography { FontFamily = ["Space Grotesk", "Manrope", "sans-serif"] },
            H4 = new H4Typography { FontFamily = ["Space Grotesk", "Manrope", "sans-serif"] },
            H5 = new H5Typography { FontFamily = ["Space Grotesk", "Manrope", "sans-serif"] },
            H6 = new H6Typography { FontFamily = ["Space Grotesk", "Manrope", "sans-serif"] },
            // Mud buttons default to UPPERCASE; normal case fits the design.
            Button = new ButtonTypography { TextTransform = "none" },
        },
    };
}
