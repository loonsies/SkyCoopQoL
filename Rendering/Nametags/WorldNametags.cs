using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnhollowerBaseLib;

namespace SkyCoopQoL;

public partial class ModEntry
{
    private Camera? tagCamera;
    private CommandBuffer? tagPass;
    private Shader? tagShader;
    private int tagAtlasGeneration;
    private Il2CppSystem.Action<Font>? tagAtlasCallback;
    private bool tagRenderFailed;
    private int tagQueuedLabels;
    private int tagGuiLabels;
    private int tagPreparedLabels;
    private int tagLastRenderFrame = -1;
    private string tagRenderStatus = "Waiting for the camera";
    private readonly List<Vector3> tagVertices = new();
    private readonly List<Vector2> tagUvs = new();
    private readonly List<Color> tagColors = new();
    private readonly List<int> tagTriangles = new();

    private void EnsureTagPass()
    {
        if (this.tagRenderFailed) return;
        Camera camera = GameManager.GetMainCamera();
        if (camera == null) { this.DetachTagPass(); return; }
        if (this.tagCamera == camera && this.tagPass != null) return;
        this.DetachTagPass();
        this.tagShader = Shader.Find("UI/Default");
        if (this.tagShader == null || !this.tagShader.isSupported) return;
        this.EnsureTagAtlasTracking();
        this.tagCamera = camera;
        this.tagPass = new CommandBuffer { name = "SkyCoopQoL_DepthTestedNameTags" };
        camera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, this.tagPass);
    }

    private void QueueWorldNameTags(Camera camera)
    {
        if (camera != this.tagCamera || this.tagPass == null) return;
        this.tagPass.Clear();
        this.tagQueuedLabels = 0;
        this.tagLastRenderFrame = Time.frameCount;
        if (this.tagRenderFailed) { this.tagRenderStatus = "Rendering failed"; return; }
        if (!this.Config.ShowNameTags) { this.tagRenderStatus = "Disabled in settings"; return; }
        if (!this.GameplayVisualsVisible) { this.tagRenderStatus = "Hidden by a menu"; return; }
        if (this.Config.NameTagsThroughWalls) { this.tagRenderStatus = "GUI overlay"; return; }
        Transform local = GameManager.GetPlayerTransform();
        if (local == null) { this.tagRenderStatus = "Waiting for the local player"; return; }
        this.tagRenderStatus = "No prepared labels in view";
        try
        {
            // Keep the world camera's color and depth attachments before image effects.
            foreach (PlayerTarget target in this.players)
            {
                if (target.TagBitmap == null || target.TagMesh == null) continue;
                if (!this.TryGetTagPlacement(camera, local, target, out Rect placement, out float depth, out Color tint)) continue;
                if (target.TagMaterial == null)
                {
                    target.TagMaterial = this.CreateWorldTagMaterial();
                    target.TagBackgroundMaterial = this.CreateWorldTagMaterial();
                    target.TagBackgroundMaterial.mainTexture = Texture2D.whiteTexture;
                    target.TagBackgroundMaterial.SetVector("_TextureSampleAdd", Vector4.zero);
                }
                target.TagMaterial.mainTexture = target.TagBitmap!.Texture;
                target.TagMaterial.SetVector("_TextureSampleAdd", Vector4.zero);
                target.TagMaterial.SetColor("_Color", new Color(1f, 1f, 1f, tint.a));
                target.TagBackgroundMaterial!.SetColor("_Color", new Color(0.025f, 0.035f, 0.055f,
                    tint.a * Mathf.Clamp01(this.Config.NameTagBackgroundOpacity / 100f)));
                Vector3 origin = camera.ScreenToWorldPoint(new Vector3(placement.x, placement.y, depth));
                float pixelScale = Vector3.Distance(origin, camera.ScreenToWorldPoint(new Vector3(placement.x, placement.y + 1f, depth)));
                Matrix4x4 matrix = Matrix4x4.TRS(origin, camera.transform.rotation, Vector3.one * pixelScale);
                if (this.Config.NameTagBackgroundOpacity > 0) this.tagPass.DrawMesh(target.TagMesh!, matrix, target.TagBackgroundMaterial, 0, 0);
                this.tagPass.DrawMesh(target.TagMesh!, matrix, target.TagMaterial, 0, 0);
                this.tagQueuedLabels++;
            }
            if (this.tagQueuedLabels > 0) this.tagRenderStatus = "Drawing";
        }
        catch (Exception ex)
        {
            this.tagPass.Clear();
            this.tagRenderFailed = true;
            MelonLoader.MelonLogger.Error("Nametags stopped after a rendering error: " + ex);
        }
    }

    private Material CreateWorldTagMaterial()
    {
        Material material = new Material(this.tagShader!);
        material.SetInt("unity_GUIZTestMode", (int)CompareFunction.LessEqual);
        material.SetInt("_StencilComp", (int)CompareFunction.Always);
        material.SetInt("_StencilWriteMask", 0);
        material.SetInt("_ColorMask", 15);
        material.DisableKeyword("UNITY_UI_CLIP_RECT");
        material.DisableKeyword("UNITY_UI_ALPHACLIP");
        return material;
    }

    private bool TryGetTagPlacement(Camera camera, Transform local, PlayerTarget target, out Rect placement, out float depth, out Color tint)
    {
        placement = default; depth = 0f; tint = default;
        if (!this.TryGetBounds(target, out Bounds bounds)) return false;
        float distance = Vector3.Distance(local.position, target.Root.transform.position);
        if (!this.IsTagInRange(distance)) return false;
        tint = this.GetNameTagColor(distance);
        if (tint.a <= 0.001f) return false;
        Vector3 anchor = camera.WorldToScreenPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
        Rect viewport = camera.pixelRect;
        if (!IsFinite(anchor) || anchor.z <= camera.nearClipPlane || !viewport.Contains(new Vector2(anchor.x, anchor.y))) return false;
        float width = target.TagSize.x, height = target.TagSize.y;
        float x = anchor.x - width * 0.5f;
        float y = anchor.y + Mathf.Clamp(this.Config.NameTagOffset, 0, 80);
        if (this.Config.NameTagClampToScreen)
        {
            x = Mathf.Clamp(x, viewport.xMin + 4f, Mathf.Max(viewport.xMin + 4f, viewport.xMax - width - 4f));
            y = Mathf.Clamp(y, viewport.yMin + 4f, Mathf.Max(viewport.yMin + 4f, viewport.yMax - height - 4f));
        }
        placement = new Rect(x, y, width, height);
        depth = anchor.z;
        return true;
    }

    private void DrawThroughWallNameTags(Camera camera, Transform local)
    {
        this.tagGuiLabels = 0;
        if (this.tagRenderFailed || !this.Config.ShowNameTags || !this.Config.NameTagsThroughWalls || !this.GameplayVisualsVisible) return;
        foreach (PlayerTarget target in this.players)
        {
            if (target.TagBitmap == null) continue;
            if (!this.TryGetTagPlacement(camera, local, target, out Rect placement, out _, out Color tint)) continue;
            placement.y = Screen.height - placement.yMax;
            this.DrawStyledNameTag(placement, target, tint);
            this.tagGuiLabels++;
        }
    }

    private void PrepareWorldNameTags()
    {
        this.tagPreparedLabels = 0;
        if (this.tagRenderFailed || !this.Config.ShowNameTags || !this.GameplayVisualsVisible) return;
        Transform local = GameManager.GetPlayerTransform();
        if (local == null) return;
        try
        {
            Font? font = this.GetNameTagFont(Mathf.Clamp(this.Config.NameTagFont, 0, NameTagFonts.Length - 1));
            if (font == null) { this.tagRenderStatus = "Font unavailable"; return; }
            FontStyle style = (FontStyle)Mathf.Clamp(this.Config.NameTagFontStyle, 0, 3);
            // Font readback must finish before the camera starts rendering.
            foreach (PlayerTarget target in this.players)
            {
                if (!this.TryGetBounds(target, out _)) continue;
                float distance = Vector3.Distance(local.position, target.Root.transform.position);
                if (!this.IsTagInRange(distance)) continue;
                this.UpdateWorldTagText(target, distance);
                font.RequestCharactersInTexture(target.TagText + "0123456789m ", this.GetNameTagFontSize(distance), style);
            }
            foreach (PlayerTarget target in this.players)
            {
                if (!this.TryGetBounds(target, out _)) continue;
                float distance = Vector3.Distance(local.position, target.Root.transform.position);
                if (!this.IsTagInRange(distance)) continue;
                Color tint = this.GetNameTagColor(distance);
                if (tint.a <= 0.001f) continue;
                this.EnsureTagBitmap(target, font, this.GetNameTagFontSize(distance), style, tint);
                if (!this.Config.NameTagsThroughWalls && target.TagMesh == null) this.BuildTagQuad(target);
                this.tagPreparedLabels++;
            }
        }
        catch (Exception ex)
        {
            this.tagRenderFailed = true;
            if (this.tagPass != null) this.tagPass.Clear();
            MelonLoader.MelonLogger.Error("Couldn't prepare nametags: " + ex);
        }
    }

    private bool IsTagInRange(float distance) => distance <= this.Config.NameTagMaxDistance &&
        (!this.Config.NameTagFollowHighlightRange || !this.Config.LimitDistance ||
        (distance >= this.Config.MinDistance && distance <= this.Config.MaxDistance));

    private void UpdateWorldTagText(PlayerTarget target, float distance)
    {
        int meters = this.Config.NameTagShowDistance ? Mathf.RoundToInt(distance) : -1;
        int limit = Mathf.Clamp(this.Config.NameTagMaxNameLength, 8, 64);
        if (target.TagSourceName == target.Name && target.TagDistance == meters && target.TagNameLimit == limit &&
            target.TagShowDistance == this.Config.NameTagShowDistance && target.TagNewLine == this.Config.NameTagDistanceOnNewLine) return;
        string name = string.IsNullOrWhiteSpace(target.Name) ? "Player" : target.Name.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        if (name.Length > limit)
        {
            int end = limit - 1;
            if (char.IsHighSurrogate(name[end - 1])) end--;
            name = name.Substring(0, end) + "\u2026";
        }
        target.TagText = name + (meters >= 0 ? (this.Config.NameTagDistanceOnNewLine ? "\n" : "  ") + meters + "m" : "");
        target.TagSourceName = target.Name; target.TagDistance = meters; target.TagNameLimit = limit;
        target.TagShowDistance = this.Config.NameTagShowDistance; target.TagNewLine = this.Config.NameTagDistanceOnNewLine;
    }

    private void BuildTagQuad(PlayerTarget target)
    {
        this.tagVertices.Clear(); this.tagUvs.Clear(); this.tagColors.Clear(); this.tagTriangles.Clear();
        this.AddTagQuad(0f, 0f, target.TagSize.x, target.TagSize.y,
            Vector2.zero, Vector2.right, Vector2.one, Vector2.up, Color.white);
        target.TagMesh = new Mesh { name = "SkyCoopQoL_LabelBitmap" };
        this.UploadTagMesh(target.TagMesh);
    }

    private void AddTagQuad(float left, float bottom, float right, float top, Vector2 bl, Vector2 br, Vector2 tr, Vector2 tl, Color color)
    {
        int start = this.tagVertices.Count;
        this.tagVertices.Add(new Vector3(left, bottom, 0)); this.tagVertices.Add(new Vector3(right, bottom, 0));
        this.tagVertices.Add(new Vector3(right, top, 0)); this.tagVertices.Add(new Vector3(left, top, 0));
        this.tagUvs.Add(bl); this.tagUvs.Add(br); this.tagUvs.Add(tr); this.tagUvs.Add(tl);
        for (int i = 0; i < 4; i++) this.tagColors.Add(color);
        this.tagTriangles.Add(start); this.tagTriangles.Add(start + 1); this.tagTriangles.Add(start + 2);
        this.tagTriangles.Add(start); this.tagTriangles.Add(start + 2); this.tagTriangles.Add(start + 3);
    }

    private void UploadTagMesh(Mesh mesh)
    {
        mesh.Clear();
        mesh.vertices = (Il2CppStructArray<Vector3>)this.tagVertices.ToArray();
        mesh.uv = (Il2CppStructArray<Vector2>)this.tagUvs.ToArray();
        mesh.colors = (Il2CppStructArray<Color>)this.tagColors.ToArray();
        mesh.triangles = (Il2CppStructArray<int>)this.tagTriangles.ToArray();
        mesh.RecalculateBounds();
    }

    private void ReleaseWorldTag(PlayerTarget target)
    {
        if (target.TagMesh != null) UnityEngine.Object.Destroy(target.TagMesh);
        if (target.TagMaterial != null) UnityEngine.Object.Destroy(target.TagMaterial);
        if (target.TagBackgroundMaterial != null) UnityEngine.Object.Destroy(target.TagBackgroundMaterial);
        if (target.TagBitmap != null) UnityEngine.Object.Destroy(target.TagBitmap.Texture);
        target.TagBitmap = null; target.TagMesh = null;
        target.TagMaterial = null; target.TagBackgroundMaterial = null;
        target.TagBitmapKey = null;
    }

    private void EnsureTagAtlasTracking()
    {
        if (this.tagAtlasCallback != null) return;
        this.tagAtlasCallback = UnhollowerRuntimeLib.DelegateSupport.ConvertDelegate<Il2CppSystem.Action<Font>>(
            new Action<Font>(_ => this.tagAtlasGeneration++));
        Font.add_textureRebuilt(this.tagAtlasCallback);
    }

    private void DetachTagPass()
    {
        if (this.tagPass != null)
        {
            if (this.tagCamera != null) this.tagCamera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, this.tagPass);
            this.tagPass.Release();
        }
        this.tagPass = null; this.tagCamera = null;
        this.tagQueuedLabels = 0;
        this.tagGuiLabels = 0;
    }
}
