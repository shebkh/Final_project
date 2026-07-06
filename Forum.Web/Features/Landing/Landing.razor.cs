// Forum.Web/Features/Landing/Landing.razor.cs
using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace Forum.Web.Features.Landing;

public partial class Landing
{
    // LOCKED §9 — the only three configurable props, wired live. Defaults match
    // the reference; exposed as parameters so the page stays tweakable/reusable.
    // The page renders as static SSR (no @rendermode): all motion is client JS
    // loaded via a self-initializing module script, so the no-JS fallback holds
    // and there's no Blazor circuit cost for this anonymous marketing page.
    [Parameter] public string Accent { get; set; } = "#4a6ee0";
    [Parameter] public int GlassBlur { get; set; } = 18;
    [Parameter] public double MotionIntensity { get; set; } = 0.6;

    // Accent + glass blur are CSS variables set inline on the page root.
    private string RootStyle => $"--accent:{Accent};--glass-blur:{GlassBlur}px;";

    // Passed to landing.js via a data-attribute (invariant culture → dot decimal).
    private string MotionIntensityValue =>
        MotionIntensity.ToString(CultureInfo.InvariantCulture);
}
