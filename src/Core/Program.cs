using Poke.Exceptions;
using Poke.Models;
using Poke.Statics;
using System.Text.Json;

namespace Poke.Core;

internal static class Program
{
    /// <summary>
    /// App version.
    /// </summary>
    public const string AppVersion = "Poke v0.1-beta";

    /// <summary>
    /// Fallback user-agent.
    /// </summary>
    public const string AppUserAgent = "Poke/0.1";

    /// <summary>
    /// Init all the things..
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    private static async Task Main(string[] args)
    {
        Console.CursorVisible = false;

        if (args.Length == 0 ||
            args.Any(n => n == "-h" ||
                          n == "--help"))
        {
            ShowProgramUsage();
            return;
        }

        var (uri, config) = await ParseCmdArgs(args);

        if (uri is null ||
            config is null ||
            !config.IsValid())
        {
            return;
        }

        var scanner = new Scanner(uri, config);

        if (!await scanner.Setup())
        {
            return;
        }

        Console.CancelKeyPress += (_, eh) =>
        {
            eh.Cancel = true;
            scanner.Abort();
        };

        await scanner.Start();
        await scanner.WriteReports();
    }

    /// <summary>
    /// Parse command-line arguments.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>URL to scan and parsed config.</returns>
    private static async Task<Tuple<Uri?, Config?>> ParseCmdArgs(string[] args)
    {
        Uri? uri = null;
        Config? config = null;

        foreach (var arg in args)
        {
            // Load config?
            if (File.Exists(arg))
            {
                try
                {
                    using var fs = File.OpenRead(arg);

                    config = await JsonSerializer.DeserializeAsync<Config>(fs) ??
                        throw ConsoleObjectsException.From(
                            "Unable to parse ",
                            ConsoleColor.Yellow,
                            arg,
                            ConsoleColorEx.ResetColor,
                            " to valid config.");
                }
                catch (Exception ex)
                {
                    ConsoleEx.WriteException(ex);
                    return new(null, null);
                }
            }

            // URL to scan?
            try
            {
                uri = new Uri(arg) ??
                    throw ConsoleObjectsException.From(
                        "Unable to parse ",
                        ConsoleColor.Yellow,
                        arg,
                        ConsoleColorEx.ResetColor,
                        " to valid URL.");
            }
            catch (Exception ex)
            {
                ConsoleEx.WriteException(ex);
                return new(null, null);
            }
        }

        return new(uri, config ?? new Config());
    }

    /// <summary>
    /// Show program usage.
    /// </summary>
    private static void ShowProgramUsage()
    {
        ConsoleEx.Write(
            ConsoleColor.White,
            AppVersion,
            ConsoleColorEx.ResetColor,
            Environment.NewLine,
            Environment.NewLine,
            "Usage:",
            Environment.NewLine,
            ConsoleColor.White,
            "  poke ",
            ConsoleColor.Blue,
            "<url-to-scan> ",
            ConsoleColor.Green,
            "[<config-file>]",
            ConsoleColorEx.ResetColor,
            Environment.NewLine,
            Environment.NewLine,
            "Visit ",
            ConsoleColor.Yellow,
            "https://github.com/nagilum/poke",
            ConsoleColorEx.ResetColor,
            " for info about the app and config format.",
            Environment.NewLine,
            Environment.NewLine);
    }
}