using System;
using System.IO;
using Audio;
using Newtonsoft.Json;
using UnityEngine;

namespace RepoSoundboard;

public class SoundboardObject
{
    public void Play()
    {
        try
        {
            lock (_lock)
            {
                if (Provider != null)
                {
                    RepoSoundboard.Logger.LogDebug($"Sound {Name} restarted.");
                    Restart();
                    return;
                }

                RepoSoundboard.Logger.LogInfo($"Trying to play {Name} from file {Path}...");
                
                FileInfo fi = new(Path);
                if (!fi.Exists) throw new FileNotFoundException(Path);
                Provider = AudioProviderFactory.FromFile(fi, RepoSoundboard.AudioFormat);
                SoundboardAudioProvider.Push(this);
            }
        }
        catch (Exception e)
        {
            RepoSoundboard.Logger.LogWarning($"Could not play \"{Name}\": {e}");
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            SoundboardAudioProvider.Remove(this);
            Provider?.Dispose();
            Provider = null;
            RepoSoundboard.Logger.LogDebug($"Sound {Name} stopped.");
        }
    }

    public void Restart()
    {
        lock (_lock) Provider!.Position = TimeSpan.Zero;
    }

    public int Read(float[] buffer, int offset, int samples)
    {
        lock (_lock)
        {
            if (Provider == null) return 0;
            return Provider.Read(buffer, offset, samples);
        }
    }

    public bool UpdateHotKey(KeyCode newHk)
    {
        if (SoundboardPool.HasObject(this))
        {
            if (!SoundboardPool.IsHotKeyAvailableFor(newHk, this)) return false;
            SoundboardPool.UpdateHk(this, HotKey, newHk);
        }

        _hk = newHk;
        return true;
    }

    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public KeyCode HotKey
    {
        get => _hk;
        set => UpdateHotKey(value);
    }
    
    [JsonIgnore] public bool Finished => Provider == null || Provider.SamplePosition >= Provider.TotalSamples;
    [JsonIgnore] public ISamplesProvider? Provider { get; private set; }
    [JsonIgnore] private readonly object _lock = new();

    private KeyCode _hk;
}