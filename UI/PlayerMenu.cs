using UnityEngine;

namespace SkyCoopQoL;

public partial class ModEntry
{
    private bool menuOpen;

    private int selectedPlayerIndex;

    private void DrawPlayerMenu()
    {
        const int visibleRows = 12;
        this.selectedPlayerIndex = Mathf.Clamp(this.selectedPlayerIndex, 0, Mathf.Max(0, this.players.Count - 1));
        int first = Mathf.Max(0, this.selectedPlayerIndex - visibleRows + 1);
        int end = Mathf.Min(this.players.Count, first + visibleRows);
        float height = 32f + Mathf.Max(1, end - first) * 26f;
        GUI.Box(new Rect(24f, 24f, 360f, height), GUIContent.none);
        GUI.Label(new Rect(24f, 26f, 360f, 24f), "SkyCoop players", this.labelStyle);
        if (this.players.Count == 0)
            GUI.Label(new Rect(32f, 52f, 344f, 24f), "No players in this area", this.labelStyle);
        for (int i = first; i < end; i++)
            GUI.Label(new Rect(32f, 52f + (i - first) * 26f, 344f, 24f),
                (i == this.selectedPlayerIndex ? "> " : "") + this.players[i].Name, this.labelStyle);
    }

    private void UpdatePlayerMenu()
    {
        bool toggle = Input.GetKeyDown(this.Config.ToggleMenu);
        if (!toggle && InputManager.m_CurrentContext != null)
            toggle = InputManager.GetKeyDown(InputManager.m_CurrentContext, this.Config.ToggleMenu);
        if (toggle)
        {
            this.menuOpen = !this.menuOpen;
            this.selectedPlayerIndex = 0;
            return;
        }
        if (this.pickerOpen || !this.menuOpen || this.players.Count == 0) return;
        this.selectedPlayerIndex = Mathf.Clamp(this.selectedPlayerIndex, 0, this.players.Count - 1);
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            this.selectedPlayerIndex = Mathf.Max(0, this.selectedPlayerIndex - 1);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            this.selectedPlayerIndex = Mathf.Min(this.players.Count - 1, this.selectedPlayerIndex + 1);
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            PlayerTarget target = this.players[this.selectedPlayerIndex];
            // The player may have disconnected since the list refreshed.
            if (this.IsLive(target)) this.TeleportTo(target.Root.transform.position);
            this.menuOpen = false;
        }
    }
}
