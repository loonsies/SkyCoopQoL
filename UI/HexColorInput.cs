using System;
using System.Globalization;

namespace SkyCoopQoL;

internal sealed class HexColorInput
{
    internal string Value { get; private set; } = "";
    internal int Caret { get; private set; }
    internal bool AllSelected { get; private set; }

    internal static bool TryParse(string text, out int rgb)
    {
        string value = text.Trim().TrimStart('#');
        rgb = 0;
        return value.Length == 6 && int.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out rgb);
    }

    internal void SetValue(string value)
    {
        Value = value.ToUpperInvariant();
        Caret = Value.Length;
        AllSelected = false;
    }

    internal void SelectAll() { AllSelected = true; Caret = Value.Length; }
    internal void Move(int delta) { SetCaret(AllSelected ? (delta < 0 ? 0 : Value.Length) : Caret + delta); }
    internal void SetCaret(int caret) { Caret = Math.Max(0, Math.Min(Value.Length, caret)); AllSelected = false; }

    internal bool Insert(char input)
    {
        char c = char.ToUpperInvariant(input);
        if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'))) return false;
        if (AllSelected) { Value = ""; Caret = 0; AllSelected = false; }
        if (Value.Length >= 6) return false;
        Value = Value.Insert(Caret, c.ToString());
        Caret++;
        return true;
    }

    internal void Backspace()
    {
        if (AllSelected) { SetValue(""); return; }
        if (Caret == 0) return;
        Value = Value.Remove(--Caret, 1);
    }

    internal void Delete()
    {
        if (AllSelected) { SetValue(""); return; }
        if (Caret < Value.Length) Value = Value.Remove(Caret, 1);
    }

    internal bool Paste(string text)
    {
        if (!TryParse(text, out int rgb)) return false;
        SetValue(rgb.ToString("X6"));
        return true;
    }
}
