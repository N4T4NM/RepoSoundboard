using System;
using UnityEngine;

namespace RepoSoundboard;

public class SoundboardLoopback
{
    public SoundboardLoopback()
    {
        _samples = new float[RepoSoundboard.AudioFormat.SampleRate / 2];
        
        Clip = AudioClip.Create("SoundboardLoopback",
            _samples.Length,
            RepoSoundboard.AudioFormat.Channels,
            RepoSoundboard.AudioFormat.SampleRate, 
            true, OnRead, OnSetPosition);
    }

    public void Push(float[] buffer)
    {
        lock (_lock)
        {
            foreach (var sample in buffer)
            {
                _samples[_writeIdx] = sample;
                _writeIdx = (_writeIdx + 1) % _samples.Length;
                
                if (_samplesAvailable < _samples.Length) _samplesAvailable++;
                else _readIdx = (_readIdx + 1) % _samples.Length; // Overwrite oldest
            }
        }
    }

    void OnRead(float[] samples)
    {
        lock (_lock)
        {
            Array.Clear(samples, 0, samples.Length);
            int count = Math.Min(samples.Length, _samplesAvailable);
            for (int i = 0; i < count; i++)
            {
                samples[i] = _samples[_readIdx];
                _readIdx = (_readIdx + 1) % _samples.Length;
            }
            _samplesAvailable -= count;
        }
    }

    void OnSetPosition(int position) {} // Do nothing for now :)

    public AudioClip Clip { get; }

    private int _readIdx;
    private int _writeIdx;
    private int _samplesAvailable;
    private readonly float[] _samples;
    private readonly object _lock = new();
}