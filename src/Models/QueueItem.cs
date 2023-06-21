using Poke.Core;

namespace Poke.Models;

internal class QueueItem
{
    /// <summary>
    /// When the URL was added to the queue.
    /// </summary>
    public DateTimeOffset Added { get; private set; } = DateTimeOffset.Now;

    /// <summary>
    /// How long processing took.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// When processing of the queue item ended.
    /// </summary>
    public DateTimeOffset? Ended { get; set; }

    /// <summary>
    /// Errors encountered during request.
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Id of page where this URL was found.
    /// </summary>
    public Guid? FoundOn { get; set; }

    /// <summary>
    /// Id.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Links found on this page.
    /// </summary>
    public List<Uri>? Links { get; set; }

    /// <summary>
    /// When processing of the queue item started.
    /// </summary>
    public DateTimeOffset? Started { get; set; }

    /// <summary>
    /// Type of URL.
    /// </summary>
    public QueueItemType Type { get; set; }

    /// <summary>
    /// Response data.
    /// </summary>
    public QueueResponse? Response { get; set; }

    /// <summary>
    /// URL to scan.
    /// </summary>
    public Uri Url { get; private set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="url">URL to scan.</param>
    /// <param name="type">Type of URL.</param>
    public QueueItem(
        Uri url,
        QueueItemType type = QueueItemType.Resource,
        Guid? foundOn = null)
    {
        this.Type = type;
        this.Url = url;
        this.FoundOn = foundOn;
    }
}