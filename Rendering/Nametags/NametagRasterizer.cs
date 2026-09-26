using System;
using UnityEngine;
using UnhollowerBaseLib;

namespace SkyCoopQoL;

public partial class ModEntry
{
    private sealed class NametagBitmap
    {
        public int Width, Height;
        public bool HasOutline;
        public byte[] Fill = null!, Outer = null!;
        public Texture2D Texture = null!;
        public int ColorKey = -1;
    }

    private Texture2D? atlasReadback;
    private byte[]? atlasAlpha;
    private int atlasWidth, atlasHeight, atlasTextureId, atlasReadbackGeneration = -1;

    private void EnsureTagBitmap(PlayerTarget target, Font font, int size, FontStyle style, Color tint)
    {
        this.EnsureTagAtlasTracking();
        int radius = Mathf.Clamp(this.Config.NameTagOutlineWidth, 0, 3);
        int padding = Mathf.Clamp(this.Config.NameTagPadding, 0, 16);
        string key = target.TagText + "|" + font.GetInstanceID() + "|" + size + "|" + (int)style + "|" + radius + "|" + padding;
        if (target.TagBitmap == null || target.TagBitmapKey != key)
        {
            font.RequestCharactersInTexture(target.TagText + "0123456789m ", size, style);
            this.CaptureFontAtlas(font);
            NametagBitmap bitmap = this.RasterizeTag(target.TagText, font, size, style, padding, radius);
            bool resized = target.TagBitmap == null || target.TagBitmap.Width != bitmap.Width || target.TagBitmap.Height != bitmap.Height;
            if (target.TagBitmap != null) UnityEngine.Object.Destroy(target.TagBitmap.Texture);
            target.TagBitmap = bitmap;
            target.TagBitmapKey = key;
            if (resized && target.TagMesh != null)
            {
                UnityEngine.Object.Destroy(target.TagMesh);
                target.TagMesh = null;
            }
        }
        NametagBitmap current = target.TagBitmap;
        target.TagSize = new Vector2(current.Width, current.Height);
        int colorKey = Mathf.RoundToInt(tint.r * 255f) << 16 | Mathf.RoundToInt(tint.g * 255f) << 8 | Mathf.RoundToInt(tint.b * 255f);
        if (current.ColorKey == colorKey) return;
        Color32[] pixels = new Color32[current.Fill.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            int fill = current.Fill[i], outer = current.Outer[i];
            if (outer == 0)
            {
                Color edge = current.HasOutline ? new Color(0.015f, 0.02f, 0.025f) : tint;
                pixels[i] = new Color32((byte)Mathf.RoundToInt(edge.r * 255f), (byte)Mathf.RoundToInt(edge.g * 255f), (byte)Mathf.RoundToInt(edge.b * 255f), 0);
                continue;
            }
            float proportion = fill / (float)outer;
            // Bake the stroke into the image so draw opacity is applied once.
            pixels[i] = new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(0.015f, tint.r, proportion) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Lerp(0.02f, tint.g, proportion) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Lerp(0.025f, tint.b, proportion) * 255f), (byte)outer);
        }
        current.Texture.SetPixels32((Il2CppStructArray<Color32>)pixels);
        current.Texture.Apply(false, false);
        current.ColorKey = colorKey;
    }

    private void CaptureFontAtlas(Font font)
    {
        Texture atlas = font.material.mainTexture;
        int width = atlas.width, height = atlas.height, id = atlas.GetInstanceID();
        if (this.atlasAlpha != null && this.atlasTextureId == id && this.atlasWidth == width && this.atlasHeight == height &&
            this.atlasReadbackGeneration == this.tagAtlasGeneration) return;
        if (this.atlasReadback == null || this.atlasReadback.width != width || this.atlasReadback.height != height)
        {
            if (this.atlasReadback != null) UnityEngine.Object.Destroy(this.atlasReadback);
            this.atlasReadback = new Texture2D(width, height, TextureFormat.RGBA32, false);
        }
        // Read back a copy because font atlases aren't always CPU-readable.
        RenderTexture previous = RenderTexture.active;
        RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        try
        {
            Graphics.Blit(atlas, temporary);
            RenderTexture.active = temporary;
            this.atlasReadback.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            Il2CppStructArray<Color32> pixels = this.atlasReadback.GetPixels32();
            byte[] alpha = new byte[width * height];
            for (int i = 0; i < alpha.Length; i++) alpha[i] = pixels[i].a;
            this.atlasAlpha = alpha;
            this.atlasWidth = width; this.atlasHeight = height; this.atlasTextureId = id;
            this.atlasReadbackGeneration = this.tagAtlasGeneration;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
        }
    }

    private NametagBitmap RasterizeTag(string text, Font font, int size, FontStyle style, int padding, int radius)
    {
        string[] lines = text.Split('\n');
        float[] widths = new float[lines.Length];
        float maxWidth = 0f, minX = 0f, maxX = 0f, minY = 0f, maxY = 0f;
        float lineHeight = size * 1.25f;
        for (int line = 0; line < lines.Length; line++)
        {
            foreach (char c in lines[line])
                if (font.GetCharacterInfo(c, out CharacterInfo glyph, size, style)) widths[line] += glyph.advance;
            maxWidth = Mathf.Max(maxWidth, widths[line]);
        }
        maxX = maxWidth; maxY = lineHeight * lines.Length;
        // Leave room for italic overhangs, descenders and the stroke.
        for (int line = 0; line < lines.Length; line++)
        {
            float x = (maxWidth - widths[line]) * 0.5f;
            float baseline = (lines.Length - line - 1) * lineHeight + size * 0.25f;
            foreach (char c in lines[line])
            {
                if (!font.GetCharacterInfo(c, out CharacterInfo glyph, size, style)) continue;
                minX = Mathf.Min(minX, x + glyph.minX); maxX = Mathf.Max(maxX, x + glyph.maxX);
                minY = Mathf.Min(minY, baseline + glyph.minY); maxY = Mathf.Max(maxY, baseline + glyph.maxY);
                x += glyph.advance;
            }
        }
        int margin = padding + radius + 1;
        float originX = margin - Mathf.Floor(minX), originY = margin - Mathf.Floor(minY);
        int width = Mathf.Max(1, Mathf.CeilToInt(maxX) - Mathf.FloorToInt(minX) + margin * 2);
        int height = Mathf.Max(1, Mathf.CeilToInt(maxY) - Mathf.FloorToInt(minY) + margin * 2);
        if (width > SystemInfo.maxTextureSize || height > SystemInfo.maxTextureSize)
            throw new InvalidOperationException("Nametag exceeds the runtime texture size limit.");
        byte[] fill = new byte[width * height];
        for (int line = 0; line < lines.Length; line++)
        {
            float x = originX + (maxWidth - widths[line]) * 0.5f;
            float baseline = originY + (lines.Length - line - 1) * lineHeight + size * 0.25f;
            foreach (char c in lines[line])
            {
                if (!font.GetCharacterInfo(c, out CharacterInfo glyph, size, style)) continue;
                float left = x + glyph.minX, bottom = baseline + glyph.minY;
                int gw = glyph.maxX - glyph.minX, gh = glyph.maxY - glyph.minY;
                if (gw > 0 && gh > 0)
                    for (int py = Mathf.FloorToInt(bottom); py < Mathf.CeilToInt(bottom + gh); py++)
                        for (int px = Mathf.FloorToInt(left); px < Mathf.CeilToInt(left + gw); px++)
                        {
                            float u = (px + 0.5f - left) / gw, v = (py + 0.5f - bottom) / gh;
                            if (px < 0 || py < 0 || px >= width || py >= height || u < 0f || v < 0f || u > 1f || v > 1f) continue;
                            Vector2 uv = Vector2.Lerp(Vector2.Lerp(glyph.uvBottomLeft, glyph.uvBottomRight, u),
                                Vector2.Lerp(glyph.uvTopLeft, glyph.uvTopRight, u), v);
                            byte alpha = this.SampleAtlasAlpha(uv);
                            int index = py * width + px;
                            if (alpha > fill[index]) fill[index] = alpha;
                        }
                x += glyph.advance;
            }
        }
        return new NametagBitmap { Width = width, Height = height, HasOutline = radius > 0, Fill = fill, Outer = AlphaMask.Expand(fill, width, height, radius),
            Texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear } };
    }

    private byte SampleAtlasAlpha(Vector2 uv)
    {
        float x = Mathf.Clamp(uv.x * this.atlasWidth - 0.5f, 0f, this.atlasWidth - 1f);
        float y = Mathf.Clamp(uv.y * this.atlasHeight - 0.5f, 0f, this.atlasHeight - 1f);
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
        int nx = Mathf.Min(ix + 1, this.atlasWidth - 1), ny = Mathf.Min(iy + 1, this.atlasHeight - 1);
        float low = Mathf.Lerp(this.atlasAlpha![iy * this.atlasWidth + ix], this.atlasAlpha[iy * this.atlasWidth + nx], x - ix);
        float high = Mathf.Lerp(this.atlasAlpha[ny * this.atlasWidth + ix], this.atlasAlpha[ny * this.atlasWidth + nx], x - ix);
        return (byte)Mathf.RoundToInt(Mathf.Lerp(low, high, y - iy));
    }

    private void ReleaseTagRasterizer()
    {
        this.ReleaseWorldTag(this.nameTagPreview);
        if (this.tagAtlasCallback != null) Font.remove_textureRebuilt(this.tagAtlasCallback);
        this.tagAtlasCallback = null;
        if (this.atlasReadback != null) UnityEngine.Object.Destroy(this.atlasReadback);
        this.atlasReadback = null; this.atlasAlpha = null; this.atlasReadbackGeneration = -1;
    }
}
