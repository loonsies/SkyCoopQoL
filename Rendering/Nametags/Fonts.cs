using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnhollowerBaseLib;

namespace SkyCoopQoL;

public partial class ModEntry
{
    private static readonly string[] NameTagFonts = { "Default GUI", "Arial", "Segoe UI", "Consolas", "Tahoma" };
    private readonly Dictionary<int, Font?> nameTagFontCache = new();
    private readonly PlayerTarget nameTagPreview = new PlayerTarget { Name = "SkyCoop player" };
    private Font? defaultNameTagFont;
    private HashSet<string>? installedFonts;

    private Font? GetNameTagFont(int choice)
    {
        if (choice == 0) return this.GetDefaultNameTagFont();
        if (this.nameTagFontCache.TryGetValue(choice, out Font? cached)) return cached ?? this.GetDefaultNameTagFont();
        Font? font = null;
        string? failure = null;
        try
        {
            if (this.installedFonts == null)
            {
                this.installedFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string name in Font.GetOSInstalledFontNames()) this.installedFonts.Add(name);
            }
            if (this.installedFonts.Contains(NameTagFonts[choice]))
            {
                // Font(string[], int) is stripped in this build; use native creation.
                font = new Font(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<Font>.NativeClassPtr));
                Font.Internal_CreateDynamicFont(font, (Il2CppStringArray)new[] { NameTagFonts[choice] }, 16);
                if (!font.dynamic) throw new InvalidOperationException("Native font creation did not produce a dynamic font.");
            }
        }
        catch (Exception ex)
        {
            if (font != null) UnityEngine.Object.Destroy(font);
            font = null;
            failure = ex.Message;
        }
        this.nameTagFontCache.Add(choice, font);
        if (font == null) MelonLogger.Warning("Couldn't load " + NameTagFonts[choice] + ". Using the default font." +
            (failure == null ? "" : " " + failure));
        return font ?? this.GetDefaultNameTagFont();
    }

    private Font? GetDefaultNameTagFont()
    {
        // A null skin font means Unity uses its native default, not that text is unavailable.
        if (this.defaultNameTagFont == null) this.defaultNameTagFont = Font.GetDefault();
        return this.defaultNameTagFont;
    }

    private int GetNameTagFontSize(float distance)
    {
        int size = Mathf.Clamp(this.Config.NameTagFontSize, 8, 48);
        if (!this.Config.NameTagScaleWithDistance) return size;
        int minimum = Mathf.Clamp(this.Config.NameTagMinFontSize, 8, size);
        float ratio = Mathf.Clamp01(distance / Mathf.Max(1f, this.Config.NameTagMaxDistance));
        return Mathf.RoundToInt(Mathf.Lerp(size, minimum, ratio));
    }

    private Color GetNameTagColor(float distance)
    {
        float alpha = Mathf.Clamp01(this.Config.NameTagOpacity / 100f);
        if (this.Config.NameTagFadeNearby) alpha *= this.GetNearFade(distance);
        return new Color(
            Mathf.Clamp01(this.Config.NameTagRed / 255f), Mathf.Clamp01(this.Config.NameTagGreen / 255f),
            Mathf.Clamp01(this.Config.NameTagBlue / 255f), alpha);
    }

    private void ReleaseNameTagFonts()
    {
        foreach (Font? font in this.nameTagFontCache.Values)
            if (font != null) UnityEngine.Object.Destroy(font);
        this.nameTagFontCache.Clear();
        this.installedFonts = null;
    }
}
