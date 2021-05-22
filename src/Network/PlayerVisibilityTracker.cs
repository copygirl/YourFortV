using System;
using System.Collections.Generic;

public class PlayerVisibilityTracker
{
    public const int TRACK_RANGE   = 4;
    public const int UNTRACK_RANGE = 5;

    private static readonly List<(int, int)> _removedChunks
        = new List<(int, int)>((UNTRACK_RANGE * 2 + 1) * (UNTRACK_RANGE * 2 + 1));

    private readonly HashSet<(int X, int Y)> _trackingChunks = new HashSet<(int, int)>();
    private (int, int)? _previousChunkPos;

    public event Action<(int, int)> ChunkTracked;
    public event Action<(int, int)> ChunkUntracked;

    public bool IsChunkTracked((int, int) chunkPos)
        => _trackingChunks.Contains(chunkPos);

    public void Process(Player player)
    {
        var chunkPos = BlockPos.FromVector(player.GlobalPosition).ToChunkPos();
        if (chunkPos == _previousChunkPos) return;

        bool IsWithin((int X, int Y) pos, int range)
            => (pos.X >= chunkPos.X - UNTRACK_RANGE) && (pos.X <= chunkPos.X + UNTRACK_RANGE) &&
               (pos.Y >= chunkPos.Y - UNTRACK_RANGE) && (pos.Y <= chunkPos.Y + UNTRACK_RANGE);

        foreach (var pos in _trackingChunks)
            if (!IsWithin(pos, UNTRACK_RANGE))
                _removedChunks.Add(pos);
        foreach (var pos in _removedChunks) {
            _trackingChunks.Remove(pos);
            ChunkUntracked?.Invoke(pos);
        }
        _removedChunks.Clear();

        for (var x = chunkPos.X - TRACK_RANGE; x <= chunkPos.X + TRACK_RANGE; x++)
            for (var y = chunkPos.Y - TRACK_RANGE; y <= chunkPos.Y + TRACK_RANGE; y++)
                if (_trackingChunks.Add((x, y)))
                    ChunkTracked?.Invoke((x, y));

        _previousChunkPos = chunkPos;
    }

    public void Reset()
    {
        _trackingChunks.Clear();
        _previousChunkPos = null;
    }
}
