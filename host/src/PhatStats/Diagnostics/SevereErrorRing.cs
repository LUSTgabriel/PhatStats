namespace PhatStats.Diagnostics;

public sealed record SevereErrorEntry(
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    string Component,
    string Code,
    string Message,
    int OccurrenceCount);

public sealed class SevereErrorRing
{
    public const int Capacity = 5;
    private readonly object _gate = new();
    private readonly List<SevereErrorEntry> _entries = new(Capacity);

    public void Add(string component, string code, string? message)
    {
        var now = DateTimeOffset.Now;
        var cleanMessage = Sanitize(message);

        lock (_gate)
        {
            if (_entries.Count > 0)
            {
                var last = _entries[^1];
                if (string.Equals(last.Component, component, StringComparison.Ordinal) &&
                    string.Equals(last.Code, code, StringComparison.Ordinal) &&
                    string.Equals(last.Message, cleanMessage, StringComparison.Ordinal))
                {
                    _entries[^1] = last with { LastSeen = now, OccurrenceCount = last.OccurrenceCount + 1 };
                    return;
                }
            }

            if (_entries.Count == Capacity)
                _entries.RemoveAt(0);

            _entries.Add(new SevereErrorEntry(now, now, component, code, cleanMessage, 1));
        }
    }

    public IReadOnlyList<SevereErrorEntry> Snapshot()
    {
        lock (_gate)
            return _entries.ToArray();
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "No additional detail";

        var text = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return text.Length <= 240 ? text : text[..240];
    }
}
