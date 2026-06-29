namespace Erp.Domain.Common;

/// <summary>
/// Generates 64-bit, time-ordered, monotonic <see cref="long"/> identifiers
/// (Snowflake-style) client-side, so an entity's <see cref="BaseEntity.Id"/> is
/// known the moment it is constructed — before <c>SaveChanges</c>. This preserves
/// the index-friendly, pre-save-known key behaviour we had with UUID v7 while
/// honouring the company "BigInt → long" standard.
///
/// Layout (63 usable bits): 41 bits ms since a fixed epoch · 9 bits worker ·
/// 13 bits per-ms sequence. Always positive.
/// </summary>
public static class IdGenerator
{
    // 2024-01-01T00:00:00Z in Unix ms — keeps the timestamp portion small for ~69 years.
    private const long Epoch = 1_704_067_200_000L;
    private const int WorkerBits = 9;
    private const int SequenceBits = 13;
    private const long MaxSequence = (1L << SequenceBits) - 1;

    private static readonly long Worker =
        Random.Shared.Next(0, 1 << WorkerBits) & ((1L << WorkerBits) - 1);

    private static readonly object Gate = new();
    private static long _lastMs = -1;
    private static long _sequence;

    public static long NewId()
    {
        lock (Gate)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now == _lastMs)
            {
                _sequence = (_sequence + 1) & MaxSequence;
                if (_sequence == 0)
                {
                    // Sequence exhausted this ms — spin to the next millisecond.
                    while (now <= _lastMs) now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }
            }
            else
            {
                _sequence = 0;
            }
            _lastMs = now;

            return ((now - Epoch) << (WorkerBits + SequenceBits))
                   | (Worker << SequenceBits)
                   | _sequence;
        }
    }
}
