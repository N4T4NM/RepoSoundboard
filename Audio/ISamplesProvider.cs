using System;

namespace Audio;

public interface ISamplesProvider : IDisposable
{
    public int Read(float[] samples, int offset, int count);

    public AudioFormat Format { get; }
    
    public TimeSpan Duration { get; }
    public int TotalSamples { get; }
    
    public TimeSpan Position { get; set; }
    public int SamplePosition { get; set; }
}