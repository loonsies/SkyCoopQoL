using UnityEngine;

namespace SkyCoopQoL;

public partial class ModEntry
{
    private Rect GetColorPreviewRect()
    {
        const float width = 340f, height = 188f;
        return new Rect(Mathf.Max(12f, Screen.width - width - 24f),
            Mathf.Max(12f, Screen.height - height - Mathf.Clamp(this.Config.PreviewBottomMargin, 100, 360)), width, height);
    }

    private void DrawColorPreview()
    {
        const float width = 340f;
        Rect panel = this.GetColorPreviewRect();
        GUI.color = new Color(0.055f, 0.065f, 0.08f, 1f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(panel.x, panel.y + 3f, width, 22f), "Highlight color preview", this.labelStyle);
        Color tint = this.GetHighlightColor(1f);
        string hex = "#" + Mathf.RoundToInt(tint.r * 255f).ToString("X2") +
            Mathf.RoundToInt(tint.g * 255f).ToString("X2") + Mathf.RoundToInt(tint.b * 255f).ToString("X2");
        GUI.Label(new Rect(panel.x, panel.y + 24f, width, 20f), hex + " | Strength " + this.Config.HighlightStrength + "%", this.labelStyle);
        float fullDistance = Mathf.Max(1f, this.Config.NearFadeDistance);
        float fullAlpha = this.GetGhostAlpha(fullDistance);
        float nearAlpha = this.GetGhostAlpha(fullDistance * 0.5f);
        GUI.Label(new Rect(panel.x + 92f, panel.y + 43f, 110f, 20f), "Snow", this.labelStyle);
        GUI.Label(new Rect(panel.x + 218f, panel.y + 43f, 110f, 20f), "Shade", this.labelStyle);
        for (int row = 0; row < 2; row++)
        {
            float y = panel.y + 65f + row * 43f;
            float alpha = row == 0 ? fullAlpha : nearAlpha;
            string distance = (row == 0 ? fullDistance : fullDistance * 0.5f).ToString("0.#");
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 6f, y, 80f, 36f), distance + "m\n" + Mathf.RoundToInt(alpha * 100f) + "% alpha", this.labelStyle);
            for (int column = 0; column < 2; column++)
            {
                Rect sample = new Rect(panel.x + 92f + column * 126f, y, 110f, 36f);
                GUI.color = column == 0 ? new Color(0.93f, 0.96f, 0.98f, 1f) : new Color(0.1f, 0.14f, 0.18f, 1f);
                GUI.DrawTexture(sample, Texture2D.whiteTexture);
                GUI.color = new Color(0.025f, 0.035f, 0.055f, alpha * 0.35f);
                GUI.DrawTexture(new Rect(sample.x + 10f, sample.y + 4f, sample.width - 20f, sample.height - 8f), Texture2D.whiteTexture);
                GUI.color = this.GetHighlightColor(alpha);
                GUI.DrawTexture(new Rect(sample.x + 12f, sample.y + 6f, sample.width - 24f, sample.height - 12f), Texture2D.whiteTexture);
            }
        }
        GUI.color = Color.white;
        GUI.Label(new Rect(panel.x, panel.y + 153f, width, 30f), "Single surface preview\nOverlapping meshes can look stronger", this.labelStyle);
    }

    private void DrawStyledNameTag(Rect rect, PlayerTarget target, Color color)
    {
        if (color.a <= 0.001f || target.TagBitmap == null) return;
        Color original = GUI.color;
        GUI.color = new Color(0.025f, 0.035f, 0.055f, color.a * Mathf.Clamp01(this.Config.NameTagBackgroundOpacity / 100f));
        if (GUI.color.a > 0f) GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, color.a);
        GUI.DrawTexture(rect, target.TagBitmap.Texture);
        GUI.color = original;
    }

    private void DrawNameTagPreview()
    {
        int size = this.GetNameTagFontSize(25f);
        if (this.tagRenderFailed) return;
        this.UpdateWorldTagText(this.nameTagPreview, 25f);
        Font? font = this.GetNameTagFont(Mathf.Clamp(this.Config.NameTagFont, 0, NameTagFonts.Length - 1));
        if (font == null) return;
        try
        {
            this.EnsureTagBitmap(this.nameTagPreview, font, size, (FontStyle)Mathf.Clamp(this.Config.NameTagFontStyle, 0, 3), this.GetNameTagColor(25f));
        }
        catch (System.Exception ex)
        {
            this.tagRenderFailed = true;
            MelonLoader.MelonLogger.Error("Couldn't draw the nametag preview: " + ex);
            return;
        }
        float sampleHeight = Mathf.Max(36f, this.nameTagPreview.TagSize.y);
        float height = 68f + sampleHeight;
        const float width = 340f;
        float bottom = this.Config.ShowHighlightPreview ? this.GetColorPreviewRect().y - 10f
            : Screen.height - Mathf.Clamp(this.Config.PreviewBottomMargin, 100, 360);
        Rect panel = new Rect(Mathf.Max(12f, Screen.width - width - 24f), Mathf.Max(12f, bottom - height), width, height);
        Color original = GUI.color;
        GUI.color = new Color(0.055f, 0.065f, 0.08f, 1f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(panel.x, panel.y + 3f, width, 22f), "Nametag preview" + (this.Config.ShowNameTags ? "" : " (disabled)"), this.labelStyle);
        Rect sample = new Rect(panel.x + 10f, panel.y + 28f, width - 20f, sampleHeight);
        GUI.color = new Color(0.93f, 0.96f, 0.98f, 1f);
        GUI.DrawTexture(new Rect(sample.x, sample.y, sample.width * 0.5f, sample.height), Texture2D.whiteTexture);
        GUI.color = new Color(0.1f, 0.14f, 0.18f, 1f);
        GUI.DrawTexture(new Rect(sample.center.x, sample.y, sample.width * 0.5f, sample.height), Texture2D.whiteTexture);
        float tagWidth = Mathf.Min(sample.width, this.nameTagPreview.TagSize.x);
        float tagHeight = this.nameTagPreview.TagSize.y;
        this.DrawStyledNameTag(new Rect(sample.center.x - tagWidth * 0.5f, sample.center.y - tagHeight * 0.5f, tagWidth, tagHeight),
            this.nameTagPreview, this.GetNameTagColor(25f));
        GUI.color = Color.white;
        GUI.Label(new Rect(panel.x, sample.yMax + 4f, width, 28f), "25m sample | " + size + "px | Opacity " + this.Config.NameTagOpacity + "%", this.labelStyle);
        GUI.color = original;
    }
}
