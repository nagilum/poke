using Microsoft.Playwright;

namespace Poke.Models;

internal class QueueResponse
{
    /// <summary>
    /// Body size.
    /// </summary>
    public int? ContentLength { get; set; }

    /// <summary>
    /// Response headers.
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Response time.
    /// </summary>
    public TimeSpan? ResponseTime { get; set; }

    /// <summary>
    /// HTTP response status code.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// HTTP response status description.
    /// </summary>
    public string? StatusDescription { get; set; }

    /// <summary>
    /// Request telemetry.
    /// </summary>
    public RequestTimingResult? Telemetry { get; set; }
}