using System.Collections.Generic;
using Audio;

namespace RepoSoundboard;

public static class SoundboardAudioProvider
{
    public static void Push(SoundboardObject obj)
    {
        lock (_active)
        {
            if(obj.Provider is ResampledAudioProvider res) RepoSoundboard.Logger.LogDebug($"Resampling from {res.SourceFormat.SampleRate} to {res.Format.SampleRate}");
            _active.Add(obj);
        }
    }
    public static void Remove(SoundboardObject obj)
    {
        lock(_active) _active.Remove(obj);
    }
    
    public static void Mix(float[] samples, float[] soundboardLoopback)
    {
        float[] temp = new float[samples.Length];
        lock (_active)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var obj = _active[i];
                if (obj.Finished) obj.Stop();
                
                int available = obj.Read(temp, 0, temp.Length);
                if(available == 0) obj.Stop();
                
                for (int sampleIdx = 0; sampleIdx < available; sampleIdx++)
                {
                    samples[sampleIdx] += temp[sampleIdx];
                    soundboardLoopback[sampleIdx] += temp[sampleIdx];
                }
            }
        }
    }

    private static readonly List<SoundboardObject> _active = new();
}