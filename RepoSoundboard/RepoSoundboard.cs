using Audio;
using BepInEx;
using BepInEx.Logging;
using Photon.Voice.Unity;
using UnityEngine;

namespace RepoSoundboard;

[BepInPlugin("NatanM.RepoSoundboard", "RepoSoundboard", "1.0")]
public class RepoSoundboard : BaseUnityPlugin
{
    internal static RepoSoundboard Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;

    internal static AudioFormat AudioFormat { get; } = new(48000, 1);
    internal static SoundboardLoopback Loopback { get; } = new();
    
    private ManualLogSource _logger => base.Logger;
    private PlayerVoiceChat? _vc;

    private void Awake()
    {
        Instance = this;
        SoundboardPool.Load();

        // Prevent the plugin from being deleted
        this.gameObject.transform.parent = null;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        RepoSoundboardMenu.Init();
        
        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }

    void Update()
    {
        if (_vc == null)
        {
            _vc = FindAnyObjectByType<PlayerVoiceChat>();
            return;
        }
        
        if (_vc.recorder != null && _vc.recorder.SourceType == Recorder.InputSourceType.Microphone)
        {
            Logger.LogInfo("Detected native audio source. Overriding...");
            InitSoundboard();
        }
    }

    private void OnGUI()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown) return;

        if (RepoSoundboardMenu.IsCapturingHotKey) RepoSoundboardMenu.DispatchHotKey(e.keyCode);
        else if(_vc !=null && _vc.recorder.SourceType == Recorder.InputSourceType.Factory) SoundboardPool.DispatchHotKey(e.keyCode);
    }

    void InitSoundboard()
    {
        // Setup soundboard loopback
        AudioSource loopbackSrc = this.gameObject.AddComponent<AudioSource>();
        loopbackSrc.clip = Loopback.Clip;
        loopbackSrc.loop = true;
        loopbackSrc.volume = .35f;
        loopbackSrc.Play();
        
        var rec = _vc!.recorder!;
        rec.RecordingEnabled = false;
        rec.SourceType = Recorder.InputSourceType.Factory;
        rec.InputFactory = () => new SoundboardMixer(_vc);
        
        Logger.LogInfo($"Started recording.");
        rec.RestartRecording();
    }
}