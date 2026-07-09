// Forum.Web/Common/MarkdownRenderer.cs
using Markdig;
using Markdig.Renderers;

namespace Forum.Web.Common;

/// <summary>
/// Renders user-authored markdown (thread/post bodies) to HTML.
/// Bodies are stored as raw markdown; conversion happens only at display time.
///
/// Security — two guards, do not remove either:
/// 1. <c>DisableHtml()</c>: raw HTML in the markdown is escaped and shown as text.
/// 2. <c>LinkRewriter</c>: link/image URLs with unsafe schemes (javascript:, data:, …)
///    are rewritten to "#" — markdown links are NOT covered by DisableHtml.
/// </summary>
public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()                          // escape raw HTML
        .UseAutoLinks()                         // bare URLs become links
        .UseSoftlineBreakAsHardlineBreak()      // single newline = <br>, matching textarea habits
        .Build();

    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer)
        {
            LinkRewriter = url => IsSafeUrl(url) ? url : "#"
        };
        Pipeline.Setup(renderer);
        renderer.Render(Markdown.Parse(markdown, Pipeline));
        writer.Flush();
        return writer.ToString();
    }

    // Allow web/mail links plus in-app relative and fragment links; everything
    // else (javascript:, data:, vbscript:, file:, …) is neutralized.
    private static bool IsSafeUrl(string? url) =>
        !string.IsNullOrEmpty(url) && (
            url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith('#')
            || url.StartsWith('/'));
}
