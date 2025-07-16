using System;
using System.IO;
using Audio.Wav;

namespace Audio;

public static class AudioProviderFactory
{
    public static ISamplesProvider FromFile(FileInfo file, AudioFormat format)
    {
        ISamplesProvider provider = null;
        switch (file.Extension.ToLower())
        {
            case ".wav": provider = FromWavFile(file); break;
            default: throw new NotImplementedException($"Support for \"{file.Exists}\" missing");
        }

        if (provider.Format != format) provider = new ResampledAudioProvider(provider, format, true);
        
        return provider;
    }

    static WaveSamplesProvider FromWavFile(FileInfo file) => new(file.OpenRead(), false);
}