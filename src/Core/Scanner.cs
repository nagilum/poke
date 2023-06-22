using Microsoft.Playwright;
using Poke.Models;
using Poke.Statics;

namespace Poke.Core;

internal class Scanner
{
    /// <summary>
    /// How long the scan took.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// When the scan ended.
    /// </summary>
    public DateTimeOffset? Ended { get; set; }

    /// <summary>
    /// Whether to keep processing the queue.
    /// </summary>
    public bool ProcessQueue { get; set; } = true;

    /// <summary>
    /// Queue items.
    /// </summary>
    public List<QueueItem> Queue { get; set; } = new();

    /// <summary>
    /// When the scan started.
    /// </summary>
    public DateTimeOffset? Started { get; set; }

    /// <summary>
    /// Config.
    /// </summary>
    private Config Config { get; set; }

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
    /// Report path.
    /// </summary>
    private string ReportPath { get; set; }

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

        await Tools.WriteReport(
            Path.Combine(this.ReportPath, "scan.json"),
            new ScanReport(this));

        await Tools.WriteReport(
            Path.Combine(this.ReportPath, "queue.json"),
            this.Queue);

        await Tools.WriteReport(
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
                StatusDescription = Tools.GetStatusText((int)res.StatusCode)
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
                    : Tools.GetStatusText(res.Status),
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
}