using System;
using System.Buffers.Binary;
using System.IO;

namespace Audio.Wav;

/// <summary>
/// A wave writer, it may have bugs, only written for testing purposes.
/// </summary>
public class WaveWriter : IDisposable
{
    public void AddSamples(float[] samples, int offset, int count)
    {
        byte[] buffer = new byte[0x02];
        for (int i = 0; i < count; i++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(buffer, (short)(samples[offset++] * short.MaxValue));
            _ms.Write(buffer, 0, buffer.Length);
        }
    }

    public void ToWave(Stream stream, int sampleRate)
    {
        WriteChunk(stream, new(WaveChunkId.RIFF, 44 + (int)_ms.Length));
        WriteI32(stream, WaveInfo.SIG_WAVE);
        
        WriteChunk(stream, new(WaveChunkId.FMT, 0x10));
        
        WriteI16(stream, (short)WaveFormat.PCM, 0x01); // Format, Channels
        WriteI32(stream, sampleRate, 768000 / 8); // Sample rate, Byte rate
        WriteI16(stream, 0x02, 0x10); // Block align, Bits per sample
        
        _ms.Position = 0;
        WriteChunk(stream, new WaveChunk(WaveChunkId.DATA, (int)_ms.Length));
        _ms.CopyTo(stream);
    }
    public void Dispose() => _ms.Dispose();

    void WriteI16(Stream s, params short[] value)
    {
        byte[] buffer = new byte[0x02];
        foreach (var t in value)
        {
            BinaryPrimitives.WriteInt16LittleEndian(buffer, t);
            s.Write(buffer, 0, buffer.Length);
        }
    }
    void WriteI32(Stream s, params int[] value)
    {
        byte[] buffer = new byte[0x04];
        foreach (var t in value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer, t);
            s.Write(buffer, 0, buffer.Length);
        }
    }

    void WriteChunk(Stream s, WaveChunk chk) => WriteI32(s, (int)chk.Id, chk.Length);

    private readonly MemoryStream _ms = new();
}