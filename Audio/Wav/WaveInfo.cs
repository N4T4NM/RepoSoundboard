namespace Audio.Wav;

public readonly struct WaveInfo(WaveFormat format, int channels, int sampleRate, int bitRate, int blockAlign, int bitsPerSample)
{
    public readonly WaveFormat Format = format;
    public readonly int Channels = channels;
    public readonly int SampleRate = sampleRate;
    public readonly int BitRate = bitRate;
    public readonly int BlockAlign =  blockAlign;
    public readonly int BitsPerSample =  bitsPerSample;
    
    public const int SIG_WAVE =  0x45564157;
}

public enum WaveFormat : short
{
    PCM = 0x01
}
