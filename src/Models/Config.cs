using Microsoft.Playwright;
using Poke.Core;
using Poke.Exceptions;
using Poke.Statics;

namespace Poke.Models;

internal class Config
{
    /// <summary>
    /// New page options.
    /// </summary>
    public BrowserNewPageOptions? BrowserNewPageOptions { get; set; }

    /// <summary>
    /// Browser launch options.
    /// </summary>
    public BrowserTypeLaunchOptions? BrowserTypeLaunchOptions { get; set; }

    /// <summary>
    /// Page go-to options.
    /// </summary>
    public PageGotoOptions? PageGotoOptions { get; set; }

    /// <summary>
    /// Where to store the report after scanning. Defaults to current directory.
    /// </summary>
    public string ReportPath { get; set; } = Directory.GetCurrentDirectory();

    /// <summary>
    /// Which rendering engine to use.
    /// </summary>
    public RenderingEngine RenderingEngine { get; set; } = RenderingEngine.Chromium;

    /// <summary>
    /// Check to see if config is valid.
    /// </summary>
    /// <returns>Success.</returns>
    public bool IsValid()
    {
        // Validate report path.
        if (!Directory.Exists(this.ReportPath))
        {
            ConsoleEx.WriteException(
                ConsoleObjectsException.From(
                    "Report path ",
                    ConsoleColor.Yellow,
                    this.ReportPath,
                    ConsoleColorEx.ResetColor,
                    " is invalid."));

            return false;
        }

        // All good.
        return true;
    }
}