using System.Text.Json;

namespace Poke.Statics;

internal static class Tools
{
    /// <summary>
    /// Get status text for code.
    /// </summary>
    /// <param name="statusCode">Status code.</param>
    /// <returns>Status text.</returns>
    public static string? GetStatusText(int statusCode)
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
    /// Write data to disk.
    /// </summary>
    /// <param name="path">Path to save to.</param>
    /// <param name="data">Data to save.</param>
    public static async Task WriteReport(string path, object data)
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