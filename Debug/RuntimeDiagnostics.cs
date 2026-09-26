using MelonLoader;
using SkyCoop;
using UnityEngine;

namespace SkyCoopQoL;

public partial class ModEntry
{
    private void DumpRuntimePlayers()
    {
        if (!this.Config.DebugMode) return;
        bool detailed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        MelonLogger.Msg("Nametags: status=" + this.tagRenderStatus + " prepared=" + this.tagPreparedLabels +
            " queued=" + this.tagQueuedLabels + " gui=" + this.tagGuiLabels +
            " event=" + (this.Config.NameTagsThroughWalls ? "GUI" : "AfterForwardAlpha") +
            " frame=" + this.tagLastRenderFrame + " shader=" + (this.tagShader != null ? this.tagShader.name : "missing") +
            " camera=" + (this.tagCamera != null ? GetPath(this.tagCamera.transform) : "none") +
            " failed=" + this.tagRenderFailed + " atlasGeneration=" + this.tagAtlasGeneration +
            " visualsVisible=" + this.GameplayVisualsVisible + " overlay=" + InterfaceManager.IsOverlayActiveImmediate());
        foreach (Camera activeCamera in Camera.allCameras)
        {
            MelonLogger.Msg("  camera " + GetPath(activeCamera.transform) + " depth=" + activeCamera.depth + " mask=" + activeCamera.cullingMask);
            CameraGlobalRT compositor = activeCamera.GetComponent<CameraGlobalRT>();
            if (compositor != null)
                MelonLogger.Msg("  compositor world=" + (compositor.m_WorldCamera != null ? GetPath(compositor.m_WorldCamera.transform) : "none") +
                    " texture=" + (compositor.m_MainRenderTexture != null ? compositor.m_MainRenderTexture.name : "none"));
        }
        if (this.tagCamera != null)
            MelonLogger.Msg("  tag target=" + (this.tagCamera.targetTexture != null ? this.tagCamera.targetTexture.name : "screen"));
        MelonLogger.Msg("Players: " + this.players.Count + ", scene=" + MyMod.levelid + "/" + MyMod.level_guid);
        MelonLogger.Msg("Highlights: shader=" + (this.modelShader != null ? this.modelShader.name : "unavailable") +
            " camera=" + (this.renderCamera != null ? GetPath(this.renderCamera.transform) : "none") +
            " queuedDraws=" + this.queuedDraws + " preRenderFrame=" + this.lastRenderFrame + " currentFrame=" + Time.frameCount +
            " failed=" + this.renderFailed + " nearFade=" + this.Config.FadeWhenNearby + "/" + this.Config.NearFadeDistance + "m" +
            " event=" + this.modelPassEvent + " highlightThroughWalls=" + this.Config.HighlightThroughWalls +
            " nametagsThroughWalls=" + this.Config.NameTagsThroughWalls);
        MelonLogger.Msg("Font: " + NameTagFonts[Mathf.Clamp(this.Config.NameTagFont, 0, NameTagFonts.Length - 1)] + " cachedFonts=" + this.nameTagFontCache.Count +
            " atlasReadbackGeneration=" + this.atlasReadbackGeneration + " defaultFont=" +
            (this.defaultNameTagFont != null ? this.defaultNameTagFont.name : "none"));
        if (this.renderCamera != null)
            MelonLogger.Msg("  camera FOV=" + this.renderCamera.fieldOfView + " pixelSize=" + this.renderCamera.pixelWidth + "x" + this.renderCamera.pixelHeight +
                " renderingPath=" + this.renderCamera.actualRenderingPath);
        if (!detailed) MelonLogger.Msg("Hold Shift with " + this.Config.DumpPlayers + " for renderer and bone details.");
        foreach (PlayerTarget target in this.players)
        {
            if (target.Root == null) continue;
            this.ScanRenderers(target);
            MelonLogger.Msg(target.Name + ": position=" + FormatVector(target.Root.transform.position) + ", renderers=" + target.Renderers.Count);
            if (!detailed) continue;
            MelonLogger.Msg("  root " + GetPath(target.Root.transform));
            var component = target.Component;
            if (component != null)
                foreach (GameObject anchor in new[] { component.m_Player, component.body, component.clothing, component.MakenzyHead, component.AstridHead })
                    if (anchor != null) MelonLogger.Msg("  anchor " + GetPath(anchor.transform));
            foreach (Renderer renderer in target.Renderers)
            {
                if (renderer == null) continue;
                MelonLogger.Msg("  " + renderer.GetType().Name + " " + GetPath(renderer.transform) +
                    " enabled=" + renderer.enabled + " active=" + renderer.gameObject.activeInHierarchy +
                    " center=" + FormatVector(renderer.bounds.center) + " size=" + FormatVector(renderer.bounds.size));
                if (renderer is SkinnedMeshRenderer skinned)
                    foreach (Transform bone in skinned.bones)
                        if (bone != null) MelonLogger.Msg("    bone " + GetPath(bone));
            }
        }
    }

    private static bool TryProjectBounds(Camera camera, Bounds bounds, out Rect rect)
    {
        rect = default;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        float left = float.PositiveInfinity, right = float.NegativeInfinity;
        float top = float.PositiveInfinity, bottom = float.NegativeInfinity;
        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 point = camera.WorldToScreenPoint(new Vector3((corner & 1) == 0 ? min.x : max.x,
                (corner & 2) == 0 ? min.y : max.y, (corner & 4) == 0 ? min.z : max.z));
            if (!IsFinite(point) || point.z <= camera.nearClipPlane) return false;
            left = Mathf.Min(left, point.x); right = Mathf.Max(right, point.x);
            top = Mathf.Min(top, Screen.height - point.y); bottom = Mathf.Max(bottom, Screen.height - point.y);
        }
        if (right < 0f || left > Screen.width || bottom < 0f || top > Screen.height) return false;
        rect = new Rect(left, top, right - left, bottom - top);
        return rect.width > 0f && rect.height > 0f;
    }
}
