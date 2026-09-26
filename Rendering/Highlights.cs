using System;
using MelonLoader;
using SkyCoop;
using UnityEngine;
using UnityEngine.Rendering;

namespace SkyCoopQoL;

public partial class ModEntry
{
    private Shader? modelShader;

    private Camera? renderCamera;

    private CommandBuffer? modelPass;

    private CameraEvent modelPassEvent = CameraEvent.AfterEverything;

    private Camera.CameraCallback? preRenderCallback;

    private float nextShaderAttempt;

    private bool shaderWarningLogged;

    private bool renderFailed;

    private int queuedDraws;

    private int lastRenderFrame = -1;

    private void EnsureModelPass()
    {
        this.EnsureTagPass();
        if (this.renderFailed) return;
        Camera camera = GameManager.GetMainCamera();
        if (camera == null) { this.DetachModelPass(); return; }
        if (this.modelShader == null)
        {
            if (Time.unscaledTime < this.nextShaderAttempt) return;
            this.nextShaderAttempt = Time.unscaledTime + 5f;
            this.modelShader = Shader.Find("Custom/Outline Fill");
            if (this.modelShader == null && MyMod.LoadedBundle != null)
                this.modelShader = MyMod.LoadedBundle.LoadAsset<Shader>("assets/quickoutline/resources/shaders/outlinefill.shader");
            if (this.modelShader == null || !this.modelShader.isSupported)
            {
                this.modelShader = null;
                if (!this.shaderWarningLogged)
                {
                    MelonLogger.Warning("Highlights are waiting for the SkyCoop shader. Use F10 for details.");
                    this.shaderWarningLogged = true;
                }
                return;
            }
        }
        CameraEvent desiredEvent = this.Config.HighlightThroughWalls ? CameraEvent.AfterEverything : CameraEvent.BeforeImageEffects;
        if (this.renderCamera == camera && this.modelPass != null && this.modelPassEvent == desiredEvent) return;
        this.DetachModelPass();
        this.modelPass = new CommandBuffer { name = "SkyCoopQoL_ActualAvatarGhost" };
        this.renderCamera = camera;
        this.modelPassEvent = desiredEvent;
        camera.AddCommandBuffer(this.modelPassEvent, this.modelPass);
    }

    private void OnCameraPreRender(Camera camera)
    {
        this.QueueWorldNameTags(camera);
        if (this.modelPass == null || camera != this.renderCamera) return;
        this.modelPass.Clear();
        this.queuedDraws = 0;
        this.lastRenderFrame = Time.frameCount;
        if (!this.GameplayVisualsVisible || this.modelShader == null || this.renderFailed)
        {
            foreach (PlayerTarget target in this.cache.Values) RestoreSkins(target);
            return;
        }
        Transform local = GameManager.GetPlayerTransform();
        if (local == null) return;
        try
        {
            bool prepared = false;
            foreach (PlayerTarget target in this.players)
            {
                if (!this.TryGetBounds(target, out _)) { RestoreSkins(target); continue; }
                float distance = Vector3.Distance(local.position, target.Root.transform.position);
                if (this.Config.LimitDistance && (distance < this.Config.MinDistance || distance > this.Config.MaxDistance))
                { RestoreSkins(target); continue; }
                float alpha = this.GetGhostAlpha(distance);
                if (alpha <= 0.001f) { RestoreSkins(target); continue; }
                if (target.FillMaterial == null)
                {
                    target.FillMaterial = this.CreateGhostMaterial(0f);
                    target.EdgeMaterial = this.CreateGhostMaterial(2f);
                }
                target.FillMaterial.SetColor("_OutlineColor", this.GetHighlightColor(alpha));
                target.EdgeMaterial!.SetColor("_OutlineColor", new Color(0.025f, 0.035f, 0.055f, alpha * 0.35f));
                int depthTest = (int)(this.Config.HighlightThroughWalls ? CompareFunction.Always : CompareFunction.LessEqual);
                target.FillMaterial.SetInt("_ZTest", depthTest);
                target.EdgeMaterial.SetInt("_ZTest", depthTest);
                foreach (Renderer renderer in target.Renderers)
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    Mesh? mesh = null;
                    if (renderer is SkinnedMeshRenderer skinned)
                    {
                        mesh = skinned.sharedMesh;
                        int id = skinned.GetInstanceID();
                        if (!target.Skins.ContainsKey(id))
                        {
                            target.Skins.Add(id, new SkinState { Renderer = skinned, OriginalUpdateWhenOffscreen = skinned.updateWhenOffscreen });
                            // Restore this flag when the highlight is hidden or the player is removed.
                            skinned.updateWhenOffscreen = true;
                        }
                    }
                    else
                    {
                        MeshFilter filter = renderer.GetComponent<MeshFilter>();
                        if (filter != null) mesh = filter.sharedMesh;
                    }
                    if (mesh == null || mesh.subMeshCount == 0) continue;
                    if (!prepared)
                    {
                        this.modelPass.SetRenderTarget(new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));
                        // Outline Fill has a fixed stencil test; clear it after drawing the world.
                        if (this.Config.HighlightThroughWalls) this.modelPass.ClearRenderTarget(true, false, Color.clear);
                        prepared = true;
                    }
                    for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                    {
                        this.modelPass.DrawRenderer(renderer, target.EdgeMaterial, submesh, 0);
                        this.modelPass.DrawRenderer(renderer, target.FillMaterial, submesh, 0);
                        this.queuedDraws += 2;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            this.modelPass.Clear();
            this.renderFailed = true;
            foreach (PlayerTarget target in this.cache.Values) this.ReleaseTarget(target);
            MelonLogger.Error("Highlights stopped after a rendering error: " + ex);
        }
    }

    private float GetGhostAlpha(float distance)
    {
        // Both mesh faces blend, so cap each surface's opacity.
        return Mathf.Clamp01(this.Config.HighlightStrength / 100f) * 0.65f * this.GetNearFade(distance);
    }

    private float GetNearFade(float distance) => this.Config.FadeWhenNearby
        ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance / Mathf.Max(1f, this.Config.NearFadeDistance))) : 1f;

    private Color GetHighlightColor(float alpha) => new Color(Mathf.Clamp01(this.Config.HighlightRed / 255f),
        Mathf.Clamp01(this.Config.HighlightGreen / 255f), Mathf.Clamp01(this.Config.HighlightBlue / 255f), alpha);

    private Material CreateGhostMaterial(float width)
    {
        Material material = new Material(this.modelShader!);
        if (!material.HasProperty("_ZTest") || !material.HasProperty("_OutlineWidth") || !material.HasProperty("_OutlineColor"))
        {
            UnityEngine.Object.Destroy(material);
            throw new InvalidOperationException("Loaded Outline Fill shader does not expose the verified ghost properties.");
        }
        material.SetInt("_ZTest", (int)CompareFunction.Always);
        material.SetFloat("_OutlineWidth", width);
        return material;
    }

    private void DetachModelPass()
    {
        if (this.modelPass != null)
        {
            if (this.renderCamera != null) this.renderCamera.RemoveCommandBuffer(this.modelPassEvent, this.modelPass);
            this.modelPass.Release();
        }
        this.modelPass = null;
        this.renderCamera = null;
        this.queuedDraws = 0;
    }

    private static void RestoreSkins(PlayerTarget target)
    {
        foreach (SkinState skin in target.Skins.Values)
            if (skin.Renderer != null) skin.Renderer.updateWhenOffscreen = skin.OriginalUpdateWhenOffscreen;
        target.Skins.Clear();
    }
}
