using System;
using MelonLoader;
using SkyCoopQoL.Framework;
using UnityEngine;

namespace SkyCoopQoL;

public partial class ModEntry : MelonMod
{
    private readonly ModConfig Config = new();

    private bool InGame => this.Config.Enabled && GameManager.m_Instance != null && !GameManager.IsMainMenuActive();

    public override void OnInitializeMelon()
    {
        // F8 belongs to the game's debug display.
        if (this.Config.ToggleDebug == KeyCode.F8 && this.Config.ToggleDebugMotion == KeyCode.F9)
        {
            this.Config.ToggleDebug = KeyCode.F9;
            this.Config.ToggleDebugMotion = KeyCode.F11;
            this.Config.Save();
            this.Config.Reload();
        }
        this.Config.AddToModSettings(ModInfo.DisplayName);
        this.Config.UpdateDebugVisibility();
        this.preRenderCallback = new Action<Camera>(this.OnCameraPreRender);
        Camera.onPreRender += this.preRenderCallback;
        MelonLogger.Msg("Ready.");
    }

    public override void OnUpdate()
    {
        this.Config.UpdateDebugVisibility();
        this.UpdateColorPicker();
        if (!this.InGame)
        {
            this.menuOpen = false;
            this.DetachTagPass();
            this.DetachModelPass();
            this.ClearPlayers();
            this.RemoveDebugPlayer();
            return;
        }

        this.UpdateDebugPlayer();
        this.RefreshPlayers();
        this.EnsureModelPass();
        this.PrepareWorldNameTags();
        if (this.Config.DebugMode && Input.GetKeyDown(this.Config.DumpPlayers)) this.DumpRuntimePlayers();

        this.UpdatePlayerMenu();
    }

    public override void OnGUI() => this.DrawOverlayGui();

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        this.DetachTagPass();
        this.DetachModelPass();
        this.RemoveDebugPlayer();
        this.ClearPlayers();
        this.menuOpen = false;
        this.CloseColorPicker();
    }

    public override void OnDeinitializeMelon()
    {
        this.RemoveDebugPlayer();
        if (this.preRenderCallback != null) Camera.onPreRender -= this.preRenderCallback;
        this.preRenderCallback = null;
        this.DetachModelPass();
        this.ClearPlayers();
        this.DetachTagPass();
        this.ReleasePickerTextures();
        this.ReleaseTagRasterizer();
        this.ReleaseNameTagFonts();
    }
}
