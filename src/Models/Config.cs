using Microsoft.Playwright;
using Poke.Core;
using Poke.Exceptions;
using Poke.Statics;

namespace Poke.Models;

internal class Config
{
    /// <summary>
    /// Browser launch options.
    /// </summary>
    public BrowserTypeLaunchOptions? BrowserTypeLaunchOptions { get; set; }

    /// <summary>
    /// Rendering devices.
    /// </summary>
    public Device[] Devices { get; set; } = Array.Empty<Device>();

    /// <summary>
    /// Where to store the report after scanning. Defaults to current directory.
    /// </summary>
    public string ReportPath { get; set; } = Directory.GetCurrentDirectory();

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