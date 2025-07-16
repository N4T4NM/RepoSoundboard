using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace RepoSoundboard;

public static class SoundboardPool
{
    public static SoundboardObject Add(SoundboardObject obj)
    {
        if (!IsHotKeyAvailable(obj.HotKey))
        {
            RepoSoundboard.Logger.LogWarning($"Found object with repeating HotKey: {obj.HotKey}");
            obj.UpdateHotKey(KeyCode.None);
        }
        
        _objects.Add(obj);
        if(obj.HotKey !=  KeyCode.None) _keyMap.Add(obj.HotKey, obj);

        return obj;
    }
    public static void Remove(SoundboardObject obj)
    {
        _objects.Remove(obj);
        _keyMap.Remove(obj.HotKey, out _);
    }
    public static bool HasObject(SoundboardObject obj) => _objects.Contains(obj);

    public static void Save()
    {
        try
        {
            RepoSoundboard.Logger.LogDebug("Saving settings...");
            var settings = GetSettingsFile();
            File.WriteAllText(settings.FullName, JsonConvert.SerializeObject(_objects, new JsonSerializerSettings()
            {
                Converters = { new HotKeyConverter() },
                Formatting = Formatting.Indented
            }));
            RepoSoundboard.Logger.LogDebug("Settings saved.");
        }
        catch (Exception e)
        {
            RepoSoundboard.Logger.LogError($"Failed to write settings: {e}");
        }
    }

    public static void Load()
    {
        try
        {
            RepoSoundboard.Logger.LogDebug("Loading settings...");
            
            _objects.Clear();
            _keyMap.Clear();
            
            var settings = GetSettingsFile();
            var json = File.ReadAllText(settings.FullName);
            
            var items = JsonConvert.DeserializeObject<List<SoundboardObject>>(json, new JsonSerializerSettings()
            {
                Converters = { new HotKeyConverter() }
            });

            if (items != null)
            {
                foreach (var item in items) Add(item);
            }
            RepoSoundboard.Logger.LogDebug("Settings loaded.");
        }
        catch (Exception e)
        {
            RepoSoundboard.Logger.LogError($"Failed to read settings: {e}");
        }
    }
    
    public static SoundboardObject Get(int idx) => _objects[idx];
    public static void DispatchHotKey(KeyCode key)
    {
        if(_keyMap.TryGetValue(key, out var obj)) obj.Play();
    }

    public static bool IsHotKeyAvailable(KeyCode key) => key == KeyCode.None || !_keyMap.ContainsKey(key);
    public static bool IsHotKeyAvailableFor(KeyCode key, SoundboardObject obj)
    {
        if (key == KeyCode.None || !_keyMap.TryGetValue(key, out var found)) return true;
        return obj == found;
    }
    
    internal static void UpdateHk(SoundboardObject obj, KeyCode old, KeyCode newValue)
    {
        _keyMap.Remove(old);
        _keyMap.Add(newValue, obj);
    }

    internal static FileInfo GetSettingsFile()
    {
        try
        {
            FileInfo asmFile = new(Assembly.GetAssembly(typeof(RepoSoundboard))!.Location);
            return new FileInfo(Path.Combine(asmFile.Directory!.FullName, "soundboard_settings.json"));
        }
        catch (Exception e)
        {
            RepoSoundboard.Logger.LogError($"Failed to get settings file path. Defaulting to current working directory. Reason: {e}");
            return new("soundboard_settings.json");
        }
    }
    
    public static int Count => _objects.Count;
    
    private static readonly List<SoundboardObject> _objects = new();
    private static readonly Dictionary<KeyCode, SoundboardObject> _keyMap = new();
}

class HotKeyConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if(value is KeyCode key == false) writer.WriteNull();
        else writer.WriteValue(key.ToString());
    }
    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.Value is string strVal == false || !Enum.TryParse<KeyCode>(strVal, out var key)) return KeyCode.None;
        return key;
    }

    public override bool CanConvert(Type objectType) => objectType == typeof(KeyCode);
}