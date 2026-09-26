using UnityEngine;

namespace SkyCoopQoL;

public partial class ModEntry
{
    private GUIStyle? labelStyle;

    private bool GameplayVisualsVisible => this.InGame && !this.menuOpen && !this.pickerOpen && !this.Config.IsVisible() && !InterfaceManager.IsOverlayActiveImmediate();

    private void DrawOverlayGui()
    {
        bool inGame = this.InGame;
        bool settingsPreview = this.IsOurSettingsTabActive();
        if (!inGame && !settingsPreview) return;
        if (this.labelStyle == null)
            this.labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
        this.defaultNameTagFont = GUI.skin.label.font ?? GUI.skin.font ?? this.defaultNameTagFont;
        this.DrawColorPickerControls(settingsPreview);
        if (Event.current.type != EventType.Repaint) return;
        Color originalColor = GUI.color;
        if (!this.pickerOpen && settingsPreview)
        {
            if (this.Config.ShowHighlightPreview) this.DrawColorPreview();
            if (this.Config.ShowNameTagPreview) this.DrawNameTagPreview();
        }
        GUI.color = originalColor;
        if (!inGame) return;
        Camera camera = GameManager.GetMainCamera();
        Transform local = GameManager.GetPlayerTransform();
        if (camera != null && local != null) this.DrawThroughWallNameTags(camera, local);
        if (this.Config.DebugMode && this.Config.ShowDebugBounds && this.debugPlayer != null && this.GameplayVisualsVisible && camera != null && local != null)
        {
            foreach (PlayerTarget target in this.players)
            {
                if (!this.TryGetBounds(target, out Bounds bounds)) continue;
                float distance = Vector3.Distance(local.position, target.Root.transform.position);
                bool inRange = !this.Config.LimitDistance ||
                    (distance >= this.Config.MinDistance && distance <= this.Config.MaxDistance);

                if (this.debugPlayer != null && this.Config.ShowDebugBounds && TryProjectBounds(camera, bounds, out Rect envelope))
                {
                    GUI.color = inRange ? Color.cyan : Color.gray;
                    DrawBoundsRect(envelope);
                }
            }
        }
        GUI.color = originalColor;
        if (this.menuOpen) this.DrawPlayerMenu();
        GUI.color = originalColor;
    }
}
