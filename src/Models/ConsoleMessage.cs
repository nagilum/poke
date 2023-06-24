namespace Poke.Models;

internal class ConsoleMessage
{
    /// <summary>
    /// Arguments passed to console message.
    /// </summary>
    public string?[]? Arguments { get; set; }

    /// <summary>
    /// Text.
    /// </summary>
    public string Text { get; set; } = null!;

    /// <summary>
    /// Type.
    /// </summary>
    public string Type { get; set; } = null!;
}