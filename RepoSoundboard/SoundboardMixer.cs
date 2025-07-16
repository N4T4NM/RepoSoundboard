using System;
using System.Collections;
using Photon.Voice;
using UnityEngine;

namespace RepoSoundboard;

public class SoundboardMixer(PlayerVoiceChat voiceChat) : IAudioReader<float>
{
    void EnsureAudioDevice()
    {
	    if (VoiceChat.recorder.MicrophoneDevice.IDString != MicrophoneDevice.IDString)
	    {
		    _lastMicPos = 0;
		    this.MicrophoneDevice = VoiceChat.recorder.MicrophoneDevice;
		    this.MicrophoneClip = Microphone.Start(this.MicrophoneDevice.Name, true, 1, SamplingRate);
		    RepoSoundboard.Logger.LogInfo($"Microphone clip updated.");
	    }
    }

    public bool Read(float[] buffer)
    {
	    EnsureAudioDevice();
	    if (MicrophoneClip == null || !Microphone.IsRecording(MicrophoneDevice.Name)) return false;
	    
	    int micPos = Microphone.GetPosition(MicrophoneDevice.Name);
	    int available = micPos - _lastMicPos;;
	    
	    if(available < 0) available += MicrophoneClip.samples;
	    if (available < buffer.Length) return false;
	    
	    Array.Clear(buffer,0, buffer.Length);
	    
	    MicrophoneClip.GetData(buffer, _lastMicPos);
	    float[] soundboardLoopbackBuffer = new float[buffer.Length];
	    
	    SoundboardAudioProvider.Mix(buffer, soundboardLoopbackBuffer);
	    
	    RepoSoundboard.Loopback.Push(soundboardLoopbackBuffer);
	    
	    _lastMicPos = (_lastMicPos + buffer.Length) % MicrophoneClip.samples;
	    
	    return true;
    }

    public void Dispose()
    {
	    if(Microphone.IsRecording(MicrophoneDevice.Name)) Microphone.End(MicrophoneDevice.Name);
	    MicrophoneClip?.UnloadAudioData();
    }

    public PlayerVoiceChat VoiceChat { get; } = voiceChat;
    public DeviceInfo MicrophoneDevice { get; set; }
    public AudioClip? MicrophoneClip { get; private set; }

    public int SamplingRate => RepoSoundboard.AudioFormat.SampleRate;
    public int Channels => RepoSoundboard.AudioFormat.Channels;
    
    public string? Error { get; private set; }
    
    private int _lastMicPos;
}