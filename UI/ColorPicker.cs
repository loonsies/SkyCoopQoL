using System;
using System.Reflection;
using UnityEngine;
using UnhollowerBaseLib;

namespace SkyCoopQoL;

public partial class ModEntry
{
    private bool pickerOpen;
    private bool pickerNameTag;
    private float pickerHue, pickerSaturation, pickerValue;
    private int pickerAxis;
    private Texture2D? pickerSquare, pickerStrip;
    private float pickerTextureHue = -1f;
    private static readonly Color[] pickerPresets = { Color.white, Color.black, Color.red,
        new Color(1f, 0.55f, 0f), Color.yellow, Color.green, Color.cyan, new Color(0.6f, 0.3f, 1f) };
    private static readonly MethodInfo? settingsNotifyMethod = typeof(ModSettings.JsonModSettings).Assembly
        .GetType("ModSettings.ModSettingsGUI")?.GetMethod("NotifySettingsNeedConfirmation", BindingFlags.Instance | BindingFlags.NonPublic);

    private void UpdateColorPicker()
    {
        if (!this.IsOurSettingsTabActive()) this.CloseColorPicker();
    }

    private void CloseColorPicker()
    {
        this.pickerOpen = false;
        this.pickerHexEditing = false;
        this.pickerHexInvalid = false;
    }

    private void OpenColorPicker(bool nametag)
    {
        if (!this.IsOurSettingsTabActive()) return;
        this.pickerNameTag = nametag;
        this.pickerOpen = true;
        Color color = nametag ? this.GetNameTagColor(1000f) : this.GetHighlightColor(1f);
        Color.RGBToHSV(color, out this.pickerHue, out this.pickerSaturation, out this.pickerValue);
        this.pickerHex.SetValue(ColorHex(color));
        this.pickerHexEditing = false;
        this.pickerHexInvalid = false;
    }

    private static string ColorHex(Color color) => Mathf.RoundToInt(color.r * 255f).ToString("X2") +
        Mathf.RoundToInt(color.g * 255f).ToString("X2") + Mathf.RoundToInt(color.b * 255f).ToString("X2");

    private void ApplyPickerColor()
    {
        if (!this.IsOurSettingsTabActive()) { this.CloseColorPicker(); return; }
        Color color = Color.HSVToRGB(this.pickerHue, this.pickerSaturation, this.pickerValue);
        int red = Mathf.RoundToInt(color.r * 255f), green = Mathf.RoundToInt(color.g * 255f), blue = Mathf.RoundToInt(color.b * 255f);
        if (this.pickerNameTag)
        { this.Config.NameTagRed = red; this.Config.NameTagGreen = green; this.Config.NameTagBlue = blue; }
        else
        { this.Config.HighlightRed = red; this.Config.HighlightGreen = green; this.Config.HighlightBlue = blue; }
        this.pickerHex.SetValue(ColorHex(color));
        this.pickerHexEditing = false;
        this.pickerHexInvalid = false;
        // Leave saving and cancellation to Mod Settings.
        object? gui = settingsGuiField?.GetValue(null);
        if (gui != null) settingsNotifyMethod?.Invoke(gui, null);
        this.Config.RefreshGUI();
    }

    private void DrawColorPickerControls(bool settingsVisible)
    {
        if (!settingsVisible) { this.CloseColorPicker(); return; }
        Color previous = GUI.color;
        Color previousContent = GUI.contentColor;
        GUI.color = Color.white; GUI.contentColor = Color.white;
        if (!this.pickerOpen)
        {
            float y = Mathf.Max(12f, Screen.height - Mathf.Clamp(this.Config.PreviewBottomMargin, 100, 360) + 8f);
            float x = Mathf.Max(12f, Screen.width - 364f);
            if (GUI.Button(new Rect(x, y, 164f, 30f), "Highlight color picker")) this.OpenColorPicker(false);
            if (GUI.Button(new Rect(x + 170f, y, 170f, 30f), "Nametag color picker")) this.OpenColorPicker(true);
        }
        else
        {
            this.EnsurePickerTextures();
            float x = Mathf.Max(12f, Screen.width - 364f);
            float y = Mathf.Max(12f, Screen.height - 435f - Mathf.Clamp(this.Config.PreviewBottomMargin, 100, 360));
            Rect panel = new Rect(x, y, 340f, 435f);
            GUI.Box(panel, "Color picker");
            if (GUI.Button(new Rect(x + 10f, y + 28f, 155f, 27f), (this.pickerNameTag ? "" : "> ") + "Highlight")) this.OpenColorPicker(false);
            if (GUI.Button(new Rect(x + 175f, y + 28f, 155f, 27f), (this.pickerNameTag ? "> " : "") + "Nametag")) this.OpenColorPicker(true);
            Rect square = new Rect(x + 10f, y + 65f, 280f, 180f);
            Rect hue = new Rect(x + 302f, y + 65f, 24f, 180f);
            GUI.DrawTexture(square, this.pickerSquare);
            GUI.DrawTexture(hue, this.pickerStrip);
            Event evt = Event.current;
            if ((evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag) && evt.button == 0)
            {
                if (square.Contains(evt.mousePosition))
                {
                    this.pickerSaturation = Mathf.Clamp01((evt.mousePosition.x - square.x) / square.width);
                    this.pickerValue = 1f - Mathf.Clamp01((evt.mousePosition.y - square.y) / square.height);
                    this.ApplyPickerColor(); GUI.FocusControl(""); evt.Use();
                }
                else if (hue.Contains(evt.mousePosition))
                {
                    this.pickerHue = Mathf.Clamp01((evt.mousePosition.y - hue.y) / hue.height);
                    this.ApplyPickerColor(); GUI.FocusControl(""); evt.Use();
                }
            }
            GUI.color = Color.black;
            DrawBoundsRect(new Rect(square.x + this.pickerSaturation * square.width - 4f,
                square.y + (1f - this.pickerValue) * square.height - 4f, 9f, 9f));
            GUI.color = Color.white;
            DrawBoundsRect(new Rect(square.x + this.pickerSaturation * square.width - 3f,
                square.y + (1f - this.pickerValue) * square.height - 3f, 7f, 7f));
            GUI.DrawTexture(new Rect(hue.x - 2f, hue.y + this.pickerHue * hue.height - 1f, hue.width + 4f, 2f), Texture2D.whiteTexture);
            for (int i = 0; i < pickerPresets.Length; i++)
            {
                GUI.color = pickerPresets[i];
                if (GUI.Button(new Rect(x + 10f + i * 40f, y + 254f, 35f, 26f), ""))
                { Color.RGBToHSV(pickerPresets[i], out this.pickerHue, out this.pickerSaturation, out this.pickerValue); this.ApplyPickerColor(); }
            }
            GUI.color = Color.white;
            this.DrawPickerHexInput(new Rect(x + 10f, y + 290f, 125f, 26f));
            if (GUI.Button(new Rect(x + 143f, y + 290f, 70f, 26f), "Set hex")) this.ApplyPickerHex();
            if (GUI.Button(new Rect(x + 222f, y + 290f, 108f, 26f), "Close")) this.CloseColorPicker();
            Color selected = this.pickerNameTag ? this.GetNameTagColor(25f) : this.GetHighlightColor(this.GetGhostAlpha(Mathf.Max(1f, this.Config.NearFadeDistance)));
            for (int i = 0; i < 2; i++)
            {
                Rect sample = new Rect(x + 10f + i * 160f, y + 327f, 155f, 34f);
                GUI.color = i == 0 ? new Color(0.93f, 0.96f, 0.98f) : new Color(0.1f, 0.14f, 0.18f);
                GUI.DrawTexture(sample, Texture2D.whiteTexture);
                GUI.color = selected;
                if (this.pickerNameTag) GUI.Label(sample, "Player 25m", this.labelStyle);
                else GUI.DrawTexture(new Rect(sample.x + 10f, sample.y + 5f, sample.width - 20f, sample.height - 10f), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            string[] axes = { "Hue", "Saturation", "Brightness" };
            GUI.Label(new Rect(x + 8f, y + 366f, 324f, 62f), (this.pickerHexInvalid ? "Enter six hex digits (0-9, A-F)." : this.pickerHexEditing ? "Hex: Enter applies, Esc leaves the field." : axes[this.pickerAxis] + " | H/S/V select, arrows adjust") + "\nX: hex | Tab: target | Shift: fine | Esc: close\n" +
                "Use game Confirm/Cancel to save/discard", this.labelStyle);
            if (evt.type == EventType.KeyDown && !this.pickerHexEditing)
            {
                bool change = false, used = true;
                if (evt.keyCode == KeyCode.Escape) this.CloseColorPicker();
                else if (evt.keyCode == KeyCode.Tab) this.OpenColorPicker(!this.pickerNameTag);
                else if (evt.keyCode == KeyCode.X) this.BeginHexEditing();
                else if (evt.keyCode == KeyCode.H) this.pickerAxis = 0;
                else if (evt.keyCode == KeyCode.S) this.pickerAxis = 1;
                else if (evt.keyCode == KeyCode.V) this.pickerAxis = 2;
                else if (evt.keyCode == KeyCode.LeftArrow || evt.keyCode == KeyCode.DownArrow || evt.keyCode == KeyCode.RightArrow || evt.keyCode == KeyCode.UpArrow)
                {
                    float delta = (evt.shift ? 0.005f : 0.025f) * (evt.keyCode == KeyCode.LeftArrow || evt.keyCode == KeyCode.DownArrow ? -1f : 1f);
                    if (this.pickerAxis == 0) this.pickerHue = Mathf.Repeat(this.pickerHue + delta, 1f);
                    else if (this.pickerAxis == 1) this.pickerSaturation = Mathf.Clamp01(this.pickerSaturation + delta);
                    else this.pickerValue = Mathf.Clamp01(this.pickerValue + delta);
                    change = true;
                }
                else used = false;
                if (change) this.ApplyPickerColor();
                if (used) evt.Use();
            }
        }
        GUI.color = previous; GUI.contentColor = previousContent;
    }

    private void EnsurePickerTextures()
    {
        if (this.pickerStrip == null)
        {
            this.pickerStrip = new Texture2D(1, 180, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Color[] colors = new Color[180];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.HSVToRGB(1f - i / 179f, 1f, 1f);
            this.pickerStrip.SetPixels((Il2CppStructArray<Color>)colors); this.pickerStrip.Apply();
        }
        if (this.pickerSquare == null) this.pickerSquare = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        if (Mathf.Abs(this.pickerTextureHue - this.pickerHue) < 0.0001f) return;
        Color[] square = new Color[64 * 64];
        for (int row = 0; row < 64; row++) for (int col = 0; col < 64; col++) square[row * 64 + col] = Color.HSVToRGB(this.pickerHue, col / 63f, row / 63f);
        this.pickerSquare.SetPixels((Il2CppStructArray<Color>)square); this.pickerSquare.Apply(); this.pickerTextureHue = this.pickerHue;
    }

    private void ReleasePickerTextures()
    {
        if (this.pickerSquare != null) UnityEngine.Object.Destroy(this.pickerSquare);
        if (this.pickerStrip != null) UnityEngine.Object.Destroy(this.pickerStrip);
        this.pickerSquare = null; this.pickerStrip = null; this.pickerTextureHue = -1f;
    }
}
