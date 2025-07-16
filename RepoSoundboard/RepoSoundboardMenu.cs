using System.Collections.Generic;
using MenuLib;
using MenuLib.MonoBehaviors;
using UnityEngine;

namespace RepoSoundboard;

public static class RepoSoundboardMenu
{
    public static void Init()
    {
        MenuAPI.AddElementToSettingsMenu(p =>
        {
            MenuAPI.CreateREPOButton("Soundboard", SpawnSoundboardMenu, p, new(120, 90f));
        });
    }

    static void SpawnSoundboardMenu()
    {
        SoundboardPool.Save();
        if (IsCapturingHotKey)
        {
            RepoSoundboard.Logger.LogWarning("IsCapturingHotKey didn't properly reset.");
            IsCapturingHotKey = false; // Just in case we get a glitch
        }

        var popup = MenuAPI.CreateREPOPopupPage("Soundboard", REPOPopupPage.PresetSide.Left, false, true);
        popup.scrollView.scrollSpeed = 3f;
        popup.maskPadding = popup.maskPadding with { top = 35 };

        popup.onEscapePressed += () => true;

        CreateCfgLayout(popup);
        
        popup.AddElement(p => 
            MenuAPI.CreateREPOButton(
                "Back", 
                () => popup.ClosePage(true),
                p, new(66f, 10f)));
        
        popup.AddElement(p =>
        {
            MenuAPI.CreateREPOButton(
                "Add",
                () =>
                {
                    var obj = SoundboardPool.Add(new() { Name = "New Sound" });
                    popup.ClosePage(true);
                    OpenMenuForEntry(obj);
                }, p, new(150f, 10f)
            );
        });
        
        popup.OpenPage(false);
    }

    static void CreateCfgLayout(REPOPopupPage popup)
    {
        for (int i = 0; i < SoundboardPool.Count; i++)
        {
            var entry = SoundboardPool.Get(i);
            popup.AddElementToScrollView(p =>
            {
                var btn = MenuAPI.CreateREPOButton(entry.Name, () =>
                {
                    popup.ClosePage(false);
                    OpenMenuForEntry(entry);
                }, p);
                return btn.rectTransform;
            });
        }
    }

    static void OpenMenuForEntry(SoundboardObject entry)
    {
        var popup = MenuAPI.CreateREPOPopupPage("Entry Settings", REPOPopupPage.PresetSide.Left, false, true);
        popup.scrollView.scrollSpeed = 3f;
        popup.maskPadding = popup.maskPadding with { top = 35 };

        popup.onEscapePressed += () =>
        {
            if (IsCapturingHotKey) return false;
            SpawnSoundboardMenu();
            popup.ClosePage(true);
            return false;
        };

        CreateEntryLayout(popup, entry);
        
        popup.AddElement(p =>
        {
            var btn = MenuAPI.CreateREPOButton(
                "Back",
                () =>
                {
                    popup.ClosePage(true);
                    SpawnSoundboardMenu();
                },
                p, new(66f, 10f));
            
            entryLayoutElements.Add(btn);
        });
        popup.OpenPage(false);
    }

    static void CreateEntryLayout(REPOPopupPage popup, SoundboardObject entry)
    {
        entryLayoutElements.Clear();
        entry.Stop();
        
        popup.AddElementToScrollView(p =>
        {
            var inp = MenuAPI.CreateREPOInputField("Name", (s) => entry.Name = s, p);
            inp.inputStringSystem.SetValue(entry.Name, false);
            
            entryLayoutElements.Add(inp);
            return inp.rectTransform;
        });
        
        popup.AddElementToScrollView(p =>
        {
            var inp = MenuAPI.CreateREPOInputField("Path", (s) => entry.Path = s, p);
            inp.inputStringSystem.SetValue(entry.Path, false);

            entryLayoutElements.Add(inp);
            return inp.rectTransform;
        });

        popup.AddElementToScrollView(p =>
        {
            var button = MenuAPI.CreateREPOButton($"HotKey: {entry.HotKey.ToString()}", null, p);
            
            button.onClick = () =>
            {
                button.labelTMP.text = "HotKey: ...";
                capturingHotKeyObj = entry;
                capturingHotKeyBtn = button;
                IsCapturingHotKey = true;
                
                RepoSoundboard.Logger.LogDebug($"IsCapturingHotKey: {IsCapturingHotKey}");
                
                foreach(var el in entryLayoutElements) el.enabled = false;
            };
            
            entryLayoutElements.Add(button);
            return button.rectTransform;
        });
        
        popup.AddElementToScrollView(p =>
        {
            var button = MenuAPI.CreateREPOButton("Delete", () =>
            {
                popup.ClosePage(false);
                SoundboardPool.Remove(entry);
                SpawnSoundboardMenu();
            }, p);

            button.rectTransform.position += new Vector3(0, 10);
            button.labelTMP.color = new(.8f, .25f, .25f, 1f);
            
            entryLayoutElements.Add(button);
            return button.rectTransform;
        });

        popup.onEscapePressed += () =>
        {
            SoundboardPool.Save();
            return true;
        };
    }

    public static void DispatchHotKey(KeyCode keyCode)
    {
        if (keyCode == KeyCode.Escape)
        {
            RepoSoundboard.Logger.LogDebug($"Ignore set new key code: {keyCode}");
            RestoreAfterHotKey();
            return;
        }

        if (keyCode == KeyCode.Backspace) keyCode = KeyCode.None;
        
        RepoSoundboard.Logger.LogDebug($"Set new HotKey: {keyCode}");
        if (capturingHotKeyObj == null)
        {
            RestoreAfterHotKey();
            return;
        }

        RepoSoundboard.Logger.LogDebug(capturingHotKeyObj.UpdateHotKey(keyCode) ? "Success." : "Failed.");
        RestoreAfterHotKey();
    }

    static void RestoreAfterHotKey()
    {
        IsCapturingHotKey = false;
        
        foreach(var el in entryLayoutElements) el.enabled = true;
        capturingHotKeyBtn!.labelTMP.text = $"HotKey: {capturingHotKeyObj!.HotKey.ToString()}";
    }

    public static bool IsCapturingHotKey { get; set; }
    private static SoundboardObject? capturingHotKeyObj;
    private static REPOButton? capturingHotKeyBtn;
    private static readonly List<REPOElement> entryLayoutElements = new();
}