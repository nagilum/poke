using Poke.Core;

namespace Poke.Models;

internal class ScanReport
{
    /// <summary>
    /// Whether the scan was aborted by the user.
    /// </summary>
    public bool AbortedByUser { get; set; }

    /// <summary>
    /// How long the scan took.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// When the scan ended.
    /// </summary>
    public DateTimeOffset Ended { get; set; }

    /// <summary>
    /// List of failed requests.
    /// </summary>
    public Dictionary<string, List<string>> Failures { get; set; } = new();

    /// <summary>
    /// When the scan started.
    /// </summary>
    public DateTimeOffset Started { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="scanner">Scanner data.</param>
    /// <param name="config">Config.</param>
    public ScanReport(Scanner scanner, Config config)
    {
        this.AbortedByUser = !scanner.ProcessQueue;
        this.Duration = scanner.Ended!.Value - scanner.Started!.Value;
        this.Ended = scanner.Ended!.Value;
        this.Started = scanner.Started!.Value;

        // Add URLs that failed on at least 1 device.
        this.Failures["failed"] = scanner.Queue
            .Where(n => n.Responses.Count != config.Devices.Length)
            .Select(n => n.Url.ToString())
            .ToList();

        // Add URLs for all status codes except 200.
        var statusCodes = new List<int>();

        foreach (var item in scanner.Queue)
        {
            statusCodes.AddRange(
                item.Responses
                    .Where(n => n.StatusCode.HasValue)
                    .Select(n => n.StatusCode!.Value));
        }

        statusCodes = statusCodes
            .Where(n => n is not 200)
            .OrderBy(n => n)
            .Distinct()
            .ToList();

        foreach (var code in statusCodes)
        {
            var urls = new List<string>();

            foreach (var item in scanner.Queue)
            {
                if (item.Responses
                    .Where(n => n.StatusCode.HasValue)
                    .Any(n => n.StatusCode!.Value == code))
                {
                    urls.Add(item.Url.ToString());
                }
            }

            this.Failures[code.ToString()] = urls;
        }
    }
}