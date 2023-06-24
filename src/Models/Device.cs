using Microsoft.Playwright;
using Poke.Core;
using System.Text.Json.Serialization;

namespace Poke.Models;

internal class Device
{
    /// <summary>
    /// Id.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// New page options.
    /// </summary>
    public BrowserNewPageOptions? BrowserNewPageOptions { get; set; }

    /// <summary>
    /// Playwright browser.
    /// </summary>
    [JsonIgnore]
    public IBrowser Browser { get; set; } = null!;

    /// <summary>
    /// Playwright page.
    /// </summary>
    [JsonIgnore]
    public IPage Page { get; set; } = null!;

    /// <summary>
    /// Which rendering engine to use.
    /// </summary>
    public RenderingEngine RenderingEngine { get; set; } = RenderingEngine.Chromium;
}