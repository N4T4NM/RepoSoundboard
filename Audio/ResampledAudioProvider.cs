using System;

namespace Audio;

/// <summary>
/// A simple resampler, the audio may lose quality under some circumstances
/// </summary>
public class ResampledAudioProvider : ISamplesProvider
{
    public int Read(float[] samples, int offset, int count)
    {
        int sr = 0;
        for (int i = 0; i < count; i++)
        {
            if (_resampledChunkReadIdx >= _resampledChunkLen && !ResampleChunk()) break;
            
            samples[offset++] = _resampledChunk[_resampledChunkReadIdx++];
            sr++;
        }

        return sr;
    }
    
    public void Dispose()
    {
        if(DisposeSource) Source.Dispose();
    }

    bool ResampleChunk()
    {
        int sr = Source.Read(_srcChunk, 0, _srcChunk.Length);
        if (sr == 0)
        {
            _resampledChunkLen = 0;
            _resampledChunkReadIdx = 0;
            return false;
        }

        if (SourceFormat.Channels == 2 && Format.Channels == 1)
        {
            int wr = 0;
            for (int i = 0; i < sr; i += 2)
            {
                _srcChunk[wr++] = _srcChunk[i];
            }
            sr /= 2;
        }

        int writeIdx = 0;
        double readIdx = 0;

        while (readIdx < sr && writeIdx < _resampledChunk.Length)
        {
            _resampledChunk[writeIdx++] = _srcChunk[(int)Math.Floor(readIdx)];
            readIdx += Ratio;
        }
        
        _resampledChunkLen = writeIdx;
        _resampledChunkReadIdx = 0;
        Source.SamplePosition -= sr - (int)Math.Floor(readIdx);
        return true;
    }

    public ResampledAudioProvider(ISamplesProvider source, AudioFormat format, bool disposeSource)
    {
        Source = source;
        Format = format;
        Ratio = (double)SourceFormat.SampleRate / Format.SampleRate;
        DisposeSource = disposeSource;

        _srcChunk = new float[Format.SampleRate];
        _resampledChunk = new float[Format.SampleRate];
        TotalSamples = (int)(Source.TotalSamples / Ratio);
    }
    
    public ISamplesProvider Source { get; }
    public AudioFormat SourceFormat => Source.Format;
    public bool DisposeSource { get; }

    public AudioFormat Format { get; }
    public double Ratio { get; }

    public TimeSpan Duration => Source.Duration;

    public TimeSpan Position
    {
        get => _pos;
        set => SamplePosition = (int)(value.TotalSeconds * (Format.SampleRate * Format.Channels)); // Set sample position, as it will deal with boundaries
    }
    public int TotalSamples { get; }

    public int SamplePosition
    {
        get => _samplePos;
        set
        {
            _samplePos = Math.Clamp(value, 0, TotalSamples);
            _pos = TimeSpan.FromSeconds((double)_samplePos / (Format.SampleRate * Format.Channels));

            _resampledChunkLen = 0;
            Source.SamplePosition = (int)Math.Floor(value / Ratio);
        }
    }

    private readonly float[] _srcChunk;
    private readonly float[] _resampledChunk;
    private int _resampledChunkLen;
    private int _resampledChunkReadIdx;

    private TimeSpan _pos;
    private int _samplePos;
}