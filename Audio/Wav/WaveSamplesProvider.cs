using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace Audio.Wav;

public class WaveSamplesProvider : ISamplesProvider
{
    public int Read(float[] samples, int offset, int count)
    {
        int sr = 0;
        for (int i = 0; i < count; i++)
        {
            if(!_sampleReader(out float s)) break;
            
            samples[offset++] = s;
            sr++;
        }

        _samplePos += sr;
        _timePos = TimeSpan.FromSeconds((double)_samplePos / (Format.SampleRate * Format.Channels));
        return sr;
    }
    public void Dispose()
    {
        if(!LeaveOpen) Stream.Dispose();
    }

    bool SampleI8(out float s)
    {
        if (!GetBytesForSample(0x01))
        {
            s = 0f;
            return false;
        }

        s = _nextSamples[0x00] / (float)sbyte.MaxValue;
        return true;
    }
    bool SampleI16(out float s)
    {
        if (!GetBytesForSample(0x02))
        {
            s = 0f;
            return false;
        }
        
        float f = short.MaxValue;
        s = BinaryPrimitives.ReadInt16LittleEndian(_nextSamples) / f;
        return true;
    }
    bool SampleI24(out float s)
    {
        if (!GetBytesForSample(0x03))
        {
            s = 0f;
            return false;
        }

        float f = 8388607f; // 2 ^ 23 - 1
        int i24 = _nextSamples[0x00] | (_nextSamples[0x01] << 8) | (_nextSamples[0x02] << 16);
        
        if ((i24 & 0x800000) != 0) // Signed value
            i24 |= unchecked((int)0xFF000000);

        s = i24 / f;
        return true;
    }
    bool SampleI32(out float s)
    {
        if (!GetBytesForSample(0x04))
        {
            s = 0f;
            return false;
        }
        
        float f = int.MaxValue;
        s = BinaryPrimitives.ReadInt32LittleEndian(_nextSamples) / f;
        return true;
    }
    
    bool GetBytesForSample(int count)
    {
        if (_currentDataChunkByteIdx >= _currentDataChunkSz && !NextDataChunk()) return false;
        
        if (Stream.Read(_nextSamples, 0, count) == count) 
            return true;
        
        return false;
    }
    bool NextDataChunk()
    {
        if (_currentDataChunkIdx >= _dataChunkCount) return false;
        var chk = SeekToChunk(WaveChunkId.DATA, _currentDataChunkIdx++);

        _currentDataChunkByteIdx = 0;
        _currentDataChunkSz = chk.Length;
        return true;
    }
    
    void SetSamplePos(int sample)
    {
        _samplePos = Math.Clamp(sample, 0, TotalSamples);
        _bytePos = _samplePos * (Info.BitsPerSample / 8);
        _timePos = TimeSpan.FromSeconds((double)_samplePos / (Format.SampleRate * Format.Channels));

        SeekToDataIdx(_bytePos);
    }
    void SetBytePos(int idx)
    {
        _bytePos = Math.Clamp(idx, 0, BytesLength);
        _samplePos = _bytePos / (Info.BitsPerSample / 8);
        _timePos = TimeSpan.FromSeconds((double)_samplePos / (Format.SampleRate * Format.Channels));
        
        SeekToDataIdx(_bytePos);
    }
    void SetTimePos(TimeSpan ts)
    {
        _samplePos = Math.Clamp((int)(ts.TotalSeconds * (Format.SampleRate * Format.Channels)), 0, TotalSamples);
        _bytePos = _samplePos * (Info.BitsPerSample / 8);
        _timePos = TimeSpan.FromSeconds((double)_samplePos / (Format.SampleRate * Format.Channels));
        
        SeekToDataIdx(_bytePos);
    }

    void SeekToDataIdx(int bp)
    {
        int offset = 0;
        for (int i = 0; i < _dataChunkCount; i++)
        {
            var chk = SeekToChunk(WaveChunkId.DATA, i);
            if (bp - offset >= chk.Length)
            {
                offset += chk.Length;
                continue;
            }

            _currentDataChunkIdx = i;
            _currentDataChunkSz = chk.Length;
            _currentDataChunkByteIdx = bp - offset;
            
            Stream.Position += _currentDataChunkByteIdx;
            return;
        }
        
        _currentDataChunkIdx = _dataChunkCount;
        _currentDataChunkByteIdx = 0;
        _currentDataChunkSz = 0;
    }

    byte[] ReadAll(int count, out int br)
    {
        int offset = 0;
        byte[] buffer = new byte[count];
        while (offset < count)
        {
            br = Stream.Read(buffer, offset, count - offset);
            if(br == 0) break;
            offset += br;
        }

        br = offset;
        return buffer;
    }
    byte[] ReadAllOrException(int count)
    {
        byte[] buffer = ReadAll(count, out int br);
        if(br < count) throw new EndOfStreamException();

        return buffer;
    }

    short ReadI16() => BinaryPrimitives.ReadInt16LittleEndian(ReadAllOrException(0x02));
    int ReadI32() => BinaryPrimitives.ReadInt32LittleEndian(ReadAllOrException(0x04));
    WaveChunk ReadChunk() => new((WaveChunkId)ReadI32(), ReadI32());

    void LoadChunks()
    {
        var hdr = ReadChunk();
        if(hdr.Id != WaveChunkId.RIFF) throw new FormatException("Expected RIFF signature");
        // We can ignore hdr.Length here, it is Stream.Length - sizeof(WaveChunk)

        if(ReadI32() != WaveInfo.SIG_WAVE) throw new FormatException("Expected WAVE format");

        while (Stream.Position < Stream.Length)
        {
            var chk = ReadChunk();
            if (!_chunkMap.TryGetValue(chk.Id, out var chunkList))
            {
                chunkList = new();
                _chunkMap.Add(chk.Id, chunkList);
            }
            else
            {
                switch (chk.Id)
                {
                    case WaveChunkId.FMT:
                    case WaveChunkId.RIFF: throw new FormatException($"Repeating chunk {chk.Id}");
                }
            }
            
            chunkList.Add(new(chk, Stream.Position - 0x08));
            Stream.Position += chk.Length;
        }
    }
    WaveChunk SeekToChunk(WaveChunkId id, int index, bool seekToContent = true)
    {
        if (!_chunkMap.TryGetValue(id, out var fmt))
            throw new KeyNotFoundException($"Could not find chunk {id}");

        Stream.Position = fmt[index].Position;
        if (seekToContent) Stream.Position += 0x08; // sizeof(WaveChunk)
        
        return fmt[index].Chunk;
    }
    int ChunkCount(WaveChunkId id)
    {
        if (!_chunkMap.TryGetValue(id, out var chunkList)) return 0;
        return chunkList.Count;
    }
    
    WaveInfo LoadInfo()
    {
        var fmt = SeekToChunk(WaveChunkId.FMT, 0);
        if(fmt.Length != 16) throw new FormatException($"Format chunk has length of {fmt.Length}. Expected 16");


        WaveFormat format = (WaveFormat)ReadI16();
        int channels = ReadI16();
        int sampleRate = ReadI32();
        int bitRate = ReadI32() * 8;
        int blockAlign = ReadI16();
        int bitsPerSample = ReadI16();
        
        if(format != WaveFormat.PCM) throw new NotSupportedException("Expected PCM audio format");
        return new(format, channels, sampleRate, bitRate, blockAlign, bitsPerSample);
    }
    void LoadDataInfo(out int bytes, out int samples)
    {
        bytes = 0;
        samples = 0;
        for (int i = 0; i < _dataChunkCount; i++)
        {
            var chk = SeekToChunk(WaveChunkId.DATA, i, false);
            bytes += chk.Length;
            samples += chk.Length / (Info.BitsPerSample / 8);
        }
    }
    
    public WaveSamplesProvider(Stream stream, bool leaveOpen)
    {
        Stream = stream;
        LeaveOpen = leaveOpen;

        LoadChunks();
        Info = LoadInfo();
        Format = new(Info.SampleRate, Info.Channels);
        
        _dataChunkCount = ChunkCount(WaveChunkId.DATA);
        LoadDataInfo(out int bytes, out int samples);
        
        BytesLength = bytes;
        TotalSamples = samples;
        Duration = TimeSpan.FromSeconds((double)TotalSamples / (Info.SampleRate * Info.Channels));

        switch (Info.BitsPerSample)
        {
            case 0x08: _sampleReader = SampleI8; break;
            case 0x10: _sampleReader = SampleI16;  break;
            case 0x18: _sampleReader = SampleI24; break;
            case 0x20: _sampleReader = SampleI32; break;
            default: throw new NotSupportedException($"Unsupported bits per sample {Info.BitsPerSample}");
        }
    }
    
    public WaveInfo Info { get; }
    public Stream Stream { get; }
    public bool LeaveOpen { get; }
    public AudioFormat Format { get; }
    
    public int TotalSamples { get; }
    public int SamplePosition
    {
        get => _samplePos;
        set => SetSamplePos(value);
    }
    
    public int BytesLength { get; }
    public int BytePosition
    {
        get => _bytePos;
        set => SetBytePos(value);
    }
    
    public TimeSpan Duration { get; }
    public TimeSpan Position
    {
        get => _timePos;
        set => SetTimePos(value);
    }

    private readonly Dictionary<WaveChunkId, List<MappedWaveChunk>> _chunkMap = new();
    private readonly WaveSampleReader _sampleReader;
    
    private int _bytePos;
    private int _samplePos;
    private TimeSpan _timePos;

    private readonly int _dataChunkCount;
    private int _currentDataChunkSz = -1;
    private int _currentDataChunkByteIdx;
    private int _currentDataChunkIdx;
    private readonly byte[] _nextSamples = new byte[0x04];

    readonly struct MappedWaveChunk(WaveChunk chunk, long position)
    {
        public readonly WaveChunk Chunk = chunk;
        public readonly long Position = position;
    }

    delegate bool WaveSampleReader(out float s);
}