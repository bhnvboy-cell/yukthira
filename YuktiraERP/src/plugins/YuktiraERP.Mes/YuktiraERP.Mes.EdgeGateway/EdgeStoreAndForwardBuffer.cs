using System.Collections.Concurrent;

namespace YuktiraERP.Mes.EdgeGateway;

/// <summary>
/// Local store-and-forward buffer (72h retention target per Section 1.2 & 13.3).
/// </summary>
public class EdgeStoreAndForwardBuffer
{
    private readonly ConcurrentQueue<EdgeTagReading> _queue = new();
    private readonly TimeSpan _retention = TimeSpan.FromHours(72);

    public int Depth => _queue.Count;

    public void Enqueue(EdgeTagReading reading) => _queue.Enqueue(reading);

    public List<EdgeTagReading> DequeueBatch(int maxCount)
    {
        var batch = new List<EdgeTagReading>(Math.Min(maxCount, _queue.Count));
        var cutoff = DateTime.UtcNow - _retention;

        while (batch.Count < maxCount && _queue.TryDequeue(out var item))
        {
            if (item.SourceTimestamp >= cutoff)
                batch.Add(item);
        }

        return batch;
    }
}
