using Microsoft.Playwright;
using Poke.Models;
using Poke.Statics;
using System.Text.Json;

namespace Poke.Core;

internal class Scanner
{
    /// <summary>
    /// Config.
    /// </summary>
    private Config Config { get; set; }

    /// <summary>
    /// How long the scan took.
    /// </summary>
    private TimeSpan? Duration { get; set; }

    /// <summary>
    /// When the scan ended.
    /// </summary>
    private DateTimeOffset? Ended { get; set; }

    /// <summary>
    /// HTTP client for external and assets.
    /// </summary>
    private HttpClient HttpClient { get; set; } = new();

    /// <summary>
    /// Current queue index.
    /// </summary>
    private int Index { get; set; } = -1;

    /// <summary>
    /// Playwright page.
    /// </summary>
    private IPage Page { get; set; } = null!;

    /// <summary>
    /// Whether to keep processing the queue.
    /// </summary>
    private bool ProcessQueue { get; set; } = true;

    /// <summary>
    /// Queue items.
    /// </summary>
    private List<QueueItem> Queue { get; set; } = new();

    /// <summary>
    /// Report path.
    /// </summary>
    private string ReportPath { get; set; }

    /// <summary>
    /// When the scan started.
    /// </summary>
    private DateTimeOffset? Started { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="uri">Initial URL to scan.</param>
    /// <param name="config">Config.</param>
    public Scanner(Uri uri, Config config)
    {
        this.Config = config;
        this.Queue.Add(new(uri));
        this.ReportPath = Path.Combine(
            this.Config.ReportPath,
            "reports",
            uri.DnsSafeHost.ToLower(),
            DateTimeOffset.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
    }

    /// <summary>
    /// Abort the scan.
    /// </summary>
    public void Abort()
    {
        ConsoleEx.Write(
            ConsoleColor.Red,
            "Aborted by user!",
            Environment.NewLine);

        this.ProcessQueue = false;
    }

    /// <summary>
    /// Setup required components.
    /// </summary>
    public async Task<bool> Setup()
    {
        try
        {
            ConsoleEx.Write(
                "Setting up Playwright..",
                Environment.NewLine);

            Microsoft.Playwright.Program.Main(
                new string[]
                {
                    "install"
                });

            var instance = await Playwright.CreateAsync();
            var browser = this.Config.RenderingEngine switch
            {
                RenderingEngine.Chromium => await instance.Chromium.LaunchAsync(this.Config.BrowserTypeLaunchOptions),
                RenderingEngine.Firefox => await instance.Firefox.LaunchAsync(this.Config.BrowserTypeLaunchOptions),
                RenderingEngine.Webkit => await instance.Webkit.LaunchAsync(this.Config.BrowserTypeLaunchOptions),
                _ => throw new NotImplementedException()
            };

            this.Page = await browser.NewPageAsync(this.Config.BrowserNewPageOptions);

            if (!Directory.Exists(this.ReportPath))
            {
                Directory.CreateDirectory(this.ReportPath);
            }

            return true;
        }
        catch (Exception ex)
        {
            ConsoleEx.WriteException(ex);
            return false;
        }
    }

    /// <summary>
    /// Start scanning from the initial URL.
    /// </summary>
    public async Task Start()
    {
        this.Started = DateTimeOffset.Now;

        ConsoleEx.Write(
            "Scanning started at ",
            ConsoleColor.Yellow,
            this.Started,
            Environment.NewLine,
            Environment.NewLine);

        while (this.ProcessQueue)
        {
            this.Index++;

            if (this.Index == this.Queue.Count)
            {
                break;
            }

            await this.ProcessQueueItem(this.Queue[this.Index]);
        }

        this.Ended = DateTimeOffset.Now;
        this.Duration = this.Ended - this.Started;

        ConsoleEx.Write(
            Environment.NewLine,
            "Scanning ended at ",
            ConsoleColor.Yellow,
            this.Ended,
            ConsoleColorEx.ResetColor,
            Environment.NewLine,
            "Scanning took ",
            ConsoleColor.Yellow,
            this.Duration,
            Environment.NewLine);
    }

    /// <summary>
    /// Write report to disk.
    /// </summary>
    public async Task WriteReports()
    {
        var statusCodes = this.Queue
            .Where(n => n.Response?.StatusCode is not null)
            .Select(n => n.Response!.StatusCode!.Value)
            .OrderBy(n => n)
            .Distinct()
            .ToList();

        await this.WriteReport(
            Path.Combine(this.ReportPath, "scan.json"),
            new
            {
                this.Started,
                this.Ended,
                this.Duration,

                AbortedByUser = !this.ProcessQueue,

                StatusCodesCount = statusCodes.ToDictionary(
                    n => n,
                    n => this.Queue.Count(m => m.Response?.StatusCode == n)),

                FailedCount = this.Queue.Count(n => n.Response is null)
            });

        await this.WriteReport(
            Path.Combine(this.ReportPath, "queue.json"),
            this.Queue);

        await this.WriteReport(
            Path.Combine(this.ReportPath, "config.json"),
            this.Config);

        ConsoleEx.Write(
            "Report written to ",
            ConsoleColor.Yellow,
            this.ReportPath,
            Environment.NewLine);
    }

    /// <summary>
    /// Extract links from page.
    /// </summary>
    /// <param name="item">Queue item.</param>
    private async Task ExtractLinks(QueueItem item)
    {
        var dict = new Dictionary<string, string>
        {
            {"a", "href"},
            {"script", "src"},
            {"link", "href"},
            {"img", "src"}
        };

        item.Links ??= new();

        foreach (var (tag, attr) in dict)
        {
            try
            {
                var links = this.Page.Locator($"//{tag}[@{attr}]");
                var count = await links.CountAsync();

                for (var i = 0; i < count; i++)
                {
                    var url = await links.Nth(i).GetAttributeAsync(attr);

                    if (url is null)
                    {
                        continue;
                    }

                    var uri = new Uri(item.Url, url);

                    if (!item.Links.Contains(uri))
                    {
                        item.Links.Add(uri);
                    }

                    if (this.Queue.Any(n => n.Url == uri))
                    {
                        continue;
                    }

                    if (item.Url.IsBaseOf(uri))
                    {
                        this.Queue.Add(
                            new(
                                uri,
                                tag is "a"
                                    ? QueueItemType.Resource
                                    : QueueItemType.Asset,
                                item.Id));
                    }
                    else
                    {
                        this.Queue.Add(
                            new(
                                uri,
                                QueueItemType.External,
                                item.Id));
                    }
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Get status text for code.
    /// </summary>
    /// <param name="statusCode">Status code.</param>
    /// <returns>Status text.</returns>
    private string? GetStatusText(int statusCode)
    {
        return statusCode switch
        {
            // Information.
            100 => "Continue",
            101 => "Switching",
            102 => "Processing",
            103 => "Early Hints",

            // Success.
            200 => "Ok",
            201 => "Created",
            202 => "Accepted",
            203 => "Non-Authoritive Information",
            204 => "No Content",
            205 => "Reset Content",
            206 => "Partial Content",
            207 => "Multi-Status",
            208 => "Already Reported",
            226 => "IM Used",

            // Redirection.
            300 => "Multiple Choices",
            301 => "Moved Permanently",
            302 => "Found",
            303 => "See Other",
            304 => "Not Modified",
            305 => "Use Proxy",
            307 => "Temporary Redirect",
            308 => "Permanent Redirect",

            // Client errors.
            400 => "Bad Request",
            401 => "Unauthorized",
            402 => "Payment Required",
            403 => "Forbidden",
            404 => "Not Found",
            405 => "Method Not Allowed",
            406 => "Not Acceptable",
            407 => "Proxy Authentication Required",
            408 => "Request Timeout",
            409 => "Conflict",
            410 => "Gone",
            411 => "Length Required",
            412 => "Precondition Failed",
            413 => "Payload Too Large",
            414 => "URI Too Long",
            415 => "Unsupported Media Type",
            416 => "Range Not Satisfiable",
            417 => "Expectation Failed",
            418 => "I'm a teapot",
            421 => "Misdirected Request",
            422 => "Unprocessable Content",
            423 => "Locked",
            424 => "Failed Dependancy",
            425 => "Too Early",
            526 => "Upgrade Required",
            428 => "Precondition Required",
            429 => "Too Many Requests",
            431 => "Request Header Fields Too Large",
            451 => "Unavailable For Legal Reasons",

            // Server errors.
            500 => "Internal Server Error",
            501 => "Not Implemented",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            504 => "Gateway Timeout",
            505 => "HTTP Version Not Supported",
            506 => "Variant Also Negotiates",
            507 => "Insufficient Storage",
            508 => "Loop Detected",
            510 => "Not Extended",
            511 => "Network Authentication Required",

            // Unknown.
            _ => null
        };
    }

    /// <summary>
    /// Perform a request using HttpClient.
    /// </summary>
    /// <param name="item">Queue item.</param>
    private async Task PerformHttpClientRequest(QueueItem item)
    {
        QueueResponse? qr = null;

        try
        {
            var msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = item.Url
            };

            msg.Headers.TryAddWithoutValidation("accept", "*/*");

            if (item.FoundOn.HasValue)
            {
                var foundOn = this.Queue
                    .Find(n => n.Id == item.FoundOn.Value);

                if (foundOn is not null)
                {
                    msg.Headers.TryAddWithoutValidation("referer", foundOn.Url.ToString());
                }
            }

            msg.Headers.TryAddWithoutValidation(
                "user-agent",
                this.Config.BrowserNewPageOptions?.UserAgent ?? Program.AppUserAgent);

            var start = DateTimeOffset.Now;

            var res = await this.HttpClient.GetAsync(item.Url) ??
                throw new Exception(
                    $"Unable to get a response from {item.Url}");

            var rt = DateTimeOffset.Now - start;

            qr = new()
            {
                Headers = res.Headers.ToDictionary(n => n.Key, n => n.Value.First().ToString()),
                ResponseTime = rt,
                StatusCode = (int)res.StatusCode,
                StatusDescription = this.GetStatusText((int)res.StatusCode)
            };
        }
        catch (TimeoutException)
        {
            item.Errors ??= new();
            item.Errors.Add($"Timeout after {(DateTimeOffset.Now - item.Started!.Value).TotalMilliseconds} milliseconds.");
        }
        catch (Exception ex)
        {
            item.Errors ??= new();
            item.Errors.Add(ex.ToString());
        }

        item.Response = qr;
    }

    /// <summary>
    /// Perform a request using Playwright and do standard analysis.
    /// </summary>
    /// <param name="item">Queue item.</param>
    private async Task PerformPlaywrightRequest(QueueItem item)
    {
        QueueResponse? qr = null;

        try
        {
            var pgo = this.Config.PageGotoOptions;

            if (item.FoundOn.HasValue)
            {
                var foundOn = this.Queue
                    .Find(n => n.Id == item.FoundOn.Value);

                if (foundOn is not null)
                {
                    pgo ??= new();
                    pgo.Referer = foundOn.Url.ToString();
                }
            }

            var start = DateTimeOffset.Now;

            var res = await this.Page.GotoAsync(item.Url.ToString(), pgo) ??
                throw new Exception(
                    $"Unable to get a response from {item.Url}");

            var rt = DateTimeOffset.Now - start;

            qr = new()
            {
                Headers = await res.AllHeadersAsync(),
                ResponseTime = rt,
                StatusCode = res.Status,
                StatusDescription = !string.IsNullOrWhiteSpace(res.StatusText)
                    ? res.StatusText
                    : this.GetStatusText(res.Status),
                Telemetry = res.Request.Timing
            };

            var body = await res.BodyAsync();

            if (body != null)
            {
                qr.ContentLength = body.Length;
            }

            await this.ExtractLinks(item);
        }
        catch (TimeoutException)
        {
            item.Errors ??= new();
            item.Errors.Add($"Timeout after {(DateTimeOffset.Now - item.Started!.Value).TotalMilliseconds} milliseconds.");
        }
        catch (Exception ex)
        {
            item.Errors ??= new();
            item.Errors.Add(ex.ToString());
        }

        item.Response = qr;
    }

    /// <summary>
    /// Process queue item.
    /// </summary>
    /// <param name="item">Queue item.</param>
    private async Task ProcessQueueItem(QueueItem item)
    {
        item.Started = DateTimeOffset.Now;

        switch (item.Type)
        {
            case QueueItemType.Asset:
            case QueueItemType.External:
                await this.PerformHttpClientRequest(item);
                break;

            case QueueItemType.Resource:
                await this.PerformPlaywrightRequest(item);
                break;
        }

        item.Ended = DateTimeOffset.Now;
        item.Duration = item.Ended - item.Started;

        this.WriteLog(item);
    }

    /// <summary>
    /// Write log entry to console.
    /// </summary>
    /// <param name="item">Queue item.</param>
    private void WriteLog(QueueItem item)
    {
        var objects = new List<object>
        {
            "[",
            ConsoleColor.Yellow,
            this.Index + 1,
            ConsoleColorEx.ResetColor,
            "/",
            ConsoleColor.Yellow,
            this.Queue.Count,
            ConsoleColorEx.ResetColor,
            "] ["
        };

        if (item.Response?.StatusCode.HasValue is true)
        {
            var sc = item.Response.StatusCode.Value.ToString();

            if (sc.StartsWith("3"))
            {
                objects.Add(ConsoleColor.Yellow);
            }
            else if (sc.StartsWith("2"))
            {
                objects.Add(ConsoleColor.Green);
            }
            else
            {
                objects.Add(ConsoleColor.Red);
            }

            objects.Add(sc);
        }
        else
        {
            objects.Add(ConsoleColor.Red);
            objects.Add("ERR");
        }

        objects.Add(ConsoleColorEx.ResetColor);
        objects.Add("] ");
        objects.Add(item.Url);
        objects.Add(Environment.NewLine);

        ConsoleEx.Write(objects.ToArray());
    }

    /// <summary>
    /// Write data to disk.
    /// </summary>
    /// <param name="path">Path to save to.</param>
    /// <param name="data">Data to save.</param>
    private async Task WriteReport(
        string path,
        object data)
    {
        try
        {
            using var fs = File.OpenWrite(path);

            await JsonSerializer.SerializeAsync(
                fs,
                data,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
        }
        catch (Exception ex)
        {
            ConsoleEx.WriteException(ex);
        }
    }
}