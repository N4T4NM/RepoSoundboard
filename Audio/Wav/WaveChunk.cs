namespace Audio.Wav;

public readonly struct WaveChunk(WaveChunkId id, int length)
{
    public readonly WaveChunkId Id = id;
    public readonly int Length = length;
}

public enum WaveChunkId : int
{
    RIFF = 0x46464952,
    FMT = 0x20746D66,
    DATA = 0x61746164
}