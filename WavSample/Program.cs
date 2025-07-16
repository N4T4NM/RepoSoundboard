using Audio;
using Audio.Wav;

using WaveSamplesProvider provider = new(File.OpenRead("/home/natan/Documents/Soundboard/bem-te-vi-cantando_mono.wav"), false);
using ResampledAudioProvider resampler = new(provider, new(48000, 1), true);
using WaveWriter writer = new();

float[] samples = new float[1024];
while (true)
{
    int s = resampler.Read(samples, 0, samples.Length);
    if(s == 0) break;
    
    writer.AddSamples(samples, 0, s);
}

using FileStream fw = File.OpenWrite("/home/natan/Documents/Soundboard/bem-te-vi-cantando_mod.wav");
writer.ToWave(fw, resampler.Format.SampleRate);