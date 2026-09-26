using System;
using MelonLoader;
using UnityEngine;

namespace SkyCoopQoL;

public partial class ModEntry
{
    private readonly HexColorInput pickerHex = new();
    private bool pickerHexEditing;
    private bool pickerHexInvalid;
    private bool pickerClipboardWarning;

    private void BeginHexEditing()
    {
        this.pickerHexEditing = true;
        this.pickerHexInvalid = false;
        this.pickerHex.SelectAll();
        GUI.FocusControl("");
    }

    private void DrawPickerHexInput(Rect rect)
    {
        Event evt = Event.current;
        if (evt.type == EventType.MouseDown && evt.button == 0 && !rect.Contains(evt.mousePosition))
            this.pickerHexEditing = false;
        string value = this.pickerHex.Value;
        if (this.pickerHexEditing)
            value = this.pickerHex.AllSelected ? "[" + value + "]" : value.Insert(this.pickerHex.Caret, "|");
        // GUI.TextField uses a stripped TextEditor setter in this game.
        if (GUI.Button(rect, "#" + value)) this.BeginHexEditing();
        if (!this.pickerHexEditing || evt.type != EventType.KeyDown) return;
        this.pickerHexInvalid = false;
        if (evt.keyCode == KeyCode.Escape || evt.keyCode == KeyCode.Tab) this.pickerHexEditing = false;
        else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) this.ApplyPickerHex();
        else if ((evt.control || evt.command) && evt.keyCode == KeyCode.A) this.pickerHex.SelectAll();
        else if ((evt.control || evt.command) && (evt.keyCode == KeyCode.V || evt.keyCode == KeyCode.C))
        {
            try
            {
                if (evt.keyCode == KeyCode.V) this.pickerHexInvalid = !this.pickerHex.Paste(GUIUtility.systemCopyBuffer);
                else GUIUtility.systemCopyBuffer = this.pickerHex.Value;
            }
            catch (Exception)
            {
                if (!this.pickerClipboardWarning) MelonLogger.Warning("Clipboard unavailable. Type the hex color instead.");
                this.pickerClipboardWarning = true;
            }
        }
        else if (evt.keyCode == KeyCode.LeftArrow) this.pickerHex.Move(-1);
        else if (evt.keyCode == KeyCode.RightArrow) this.pickerHex.Move(1);
        else if (evt.keyCode == KeyCode.Home) this.pickerHex.SetCaret(0);
        else if (evt.keyCode == KeyCode.End) this.pickerHex.SetCaret(this.pickerHex.Value.Length);
        else if (evt.keyCode == KeyCode.Backspace) this.pickerHex.Backspace();
        else if (evt.keyCode == KeyCode.Delete) this.pickerHex.Delete();
        else if (!evt.control && !evt.command && !evt.alt) this.pickerHex.Insert(evt.character);
        evt.Use();
    }

    private void ApplyPickerHex()
    {
        if (!HexColorInput.TryParse(this.pickerHex.Value, out int rgb))
        {
            this.pickerHexInvalid = true;
            return;
        }
        Color.RGBToHSV(new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f),
            out this.pickerHue, out this.pickerSaturation, out this.pickerValue);
        this.ApplyPickerColor();
    }
}
