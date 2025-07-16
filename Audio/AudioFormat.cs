using System;

namespace Audio;

public readonly struct AudioFormat(int sampleRate, int channels) : IEquatable<AudioFormat>
{
    public override bool Equals(object? obj) => obj is AudioFormat format && Equals(format);
    public bool Equals(AudioFormat other)
    {
        return SampleRate == other.SampleRate && Channels == other.Channels;
    }
    public override int GetHashCode()
    {
        unchecked
        {
            return (SampleRate * 397) ^ Channels;
        }
    }

    public static bool operator ==(AudioFormat left, AudioFormat right) => left.Equals(right);
    public static bool operator !=(AudioFormat left, AudioFormat right) => !(left == right);

    public readonly int SampleRate = sampleRate;
    public readonly int Channels = channels;
}