using System.Collections.Concurrent;
using LogInspector.Models;
namespace LogInspector.Services;

/// <summary>
/// In-memory only. Uploaded logs are never written to disk; they expire after <see cref="Ttl"/>,
/// are limited to <see cref="MaxStored"/> entries and disappear on application restart.
/// </summary>
public sealed class LogStore
{
    public const int MaxStored = 20;
    public static readonly TimeSpan Ttl = TimeSpan.FromMinutes(60);
    private readonly ConcurrentDictionary<Guid, (StoredLogInfo Info, string Content)> _items = new();
    private readonly Func<DateTime> _now;

    public LogStore() : this(() => DateTime.UtcNow) { }
    public LogStore(Func<DateTime> now) => _now = now;

    public StoredLogInfo Save(string? displayName, string content)
    {
        Evict();
        var info = new StoredLogInfo(Guid.NewGuid(), SafeName(displayName), _now(), System.Text.Encoding.UTF8.GetByteCount(content));
        _items[info.Id] = (info, content);
        foreach (var old in _items.Values.OrderByDescending(x => x.Info.CreatedUtc).Skip(MaxStored)) _items.TryRemove(old.Info.Id, out _);
        return info;
    }

    public List<StoredLogInfo> List()
    {
        Evict();
        return _items.Values.Select(x => x.Info).OrderByDescending(x => x.CreatedUtc).ToList();
    }

    public (StoredLogInfo Info, string Content)? Get(Guid id)
    {
        Evict();
        return _items.TryGetValue(id, out var v) ? v : null;
    }

    public void Delete(Guid id) => _items.TryRemove(id, out _);

    private void Evict()
    {
        var limit = _now() - Ttl;
        foreach (var x in _items.Values.Where(x => x.Info.CreatedUtc < limit)) _items.TryRemove(x.Info.Id, out _);
    }

    public static string SafeName(string? name)
    {
        var n = Path.GetFileName((name ?? "").Replace('\\', '/'));
        n = new string(n.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (n.Length == 0) n = "pasted-log";
        return n.Length > 80 ? n[..80] : n;
    }
}
