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
    public ScanReport(Scanner scanner)
    {
        this.AbortedByUser = !scanner.ProcessQueue;
        this.Duration = scanner.Ended!.Value - scanner.Started!.Value;
        this.Ended = scanner.Ended!.Value;
        this.Started = scanner.Started!.Value;

        this.Failures["failed"] = scanner.Queue
            .Where(n => n.Response is null)
            .Select(n => n.Url.ToString())
            .ToList();

        var statusCodes = scanner.Queue
            .Where(n => n.Response?.StatusCode is not null &&
                        n.Response.StatusCode is not 200)
            .Select(n => n.Response!.StatusCode!.Value)
            .OrderBy(n => n)
            .Distinct()
            .ToList();

        foreach (var sc in statusCodes)
        {
            this.Failures[sc.ToString()] = scanner.Queue
                .Where(n => n.Response is not null &&
                            n.Response.StatusCode == sc)
                .Select(n => n.Url.ToString())
                .ToList();
        }
    }
}