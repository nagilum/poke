﻿using Microsoft.Playwright;

namespace Poke.Models;

internal class QueueResponse
{
    /// <summary>
    /// Console messages.
    /// </summary>
    public List<ConsoleMessage>? ConsoleMessages { get; set; }

    /// <summary>
    /// Body size.
    /// </summary>
    public int? ContentLength { get; set; }

    /// <summary>
    /// Device id.
    /// </summary>
    public Guid? DeviceId { get; set; }

    /// <summary>
    /// Device name.
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Response headers.
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Returns SSL and other security information.
    /// </summary>
    public ResponseSecurityDetailsResult? ResponseSecurityDetailsResult { get; set; }

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