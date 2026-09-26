# SkyCoop QoL 1.5.6

For The Long Dark 2.0.1, MelonLoader 0.5.7, net472 and the installed SkyCoop LTS assembly. This is a standalone project in `SkyCoopQoL`, outside `TheLongDarkMods`. From the workspace root, build using `dotnet build SkyCoopQoL/SkyCoopQoL.csproj -c Release`, or run `dotnet build -c Release` inside this folder. The build target copies the DLL to the game Mods directory and this project's `_releases` folder. Game reference paths are configured in the local `Directory.Build.props` and can be overridden using MSBuild properties. Restart the game to load a changed DLL.

## Source layout

`ModEntry.cs` contains the MelonLoader lifecycle callbacks and frame-update coordination. Feature files remain partial definitions of the same entry-point type, preserving its private state, registration and IL2CPP integration.

| Location | Responsibility |
| --- | --- |
| `Configuration/ModConfig.cs` | Persisted settings and Mod Settings attributes. |
| `Players/` | Remote-player discovery, target data, renderer bounds, cleanup and teleport validation. |
| `Rendering/Highlights.cs` | Avatar mesh passes, shader setup, depth testing, fade and skin state restoration. |
| `Rendering/Nametags/` | Font caches, label rasterization, alpha-mask outlines and depth-tested billboard quads. |
| `UI/` | Keyboard player menu, color picker, settings integration, previews and GUI drawing. |
| `Debug/` | Test avatar spawning/movement, bounds projection and runtime dumps. |
| `Utilities/RuntimeHelpers.cs` | Finite-coordinate checks, hierarchy paths and diagnostic formatting. |
| `Properties/AssemblyInfo.cs` and `ModInfo.cs` | MelonLoader registration and mod metadata. |

Build output goes to `bin/` and `_releases/`; intermediate files go to `obj/`. These generated folders are ignored by Git. Moving configuration source does not change its existing `SkyCoopQoL.Framework` namespace or JSON settings.

## Controls and colors

Debug tools are off by default. Enable them explicitly in Mod Settings to use F9/F10/F11 or the bounds display; their controls are hidden otherwise. Turning them off removes any test player.

| Key | Action |
| --- | --- |
| F7 | Player list; Up/Down or W/S selects, Enter teleports. |
| F9 | Spawn/remove the local test avatar. |
| F11 | Toggle lateral test movement. |
| F10 | Log rendering state and player summary. Hold Shift for renderer, anchor, bounds and bone details. |

Open the color picker by clicking Highlight or Nametag on this mod's settings tab. It has no global shortcut, is not available in the player list, and closes when settings close or another tab is selected. Choose a hue on the vertical strip and saturation/brightness in the square, use a preset, or enter a six-digit RGB hex code and select Set hex. Keyboard controls: Tab switches target; H/S/V selects hue, saturation or brightness; arrows adjust; Shift makes adjustments finer; Esc closes. Click the hex field or press X to edit it; Enter applies, Esc stops editing. Arrow keys, Home/End, Backspace/Delete, Ctrl+A and Ctrl+C/V are supported. The field uses managed text editing because this game strips the TextEditor setter used by GUI.TextField. Highlight and nametag RGB channels use integer values from 0 to 255, matching hex entry without percentage rounding. Existing saved channel numbers are used directly; this change does not migrate settings. Opacity remains controlled by the existing highlight strength and nametag opacity settings. Snow/shade samples include that opacity; the nametag sample includes optional fading at 25m, while highlight samples show full-strength-distance opacity for one surface. Overlapping avatar surfaces can appear stronger.

Color changes use the existing game Confirm/Cancel lifecycle. Preview toggles and bottom margin remain configurable. The picker replaces the preview panels while open. Previews and color-picker buttons appear only in Mod Settings. The player list has no debug prompts or keybind footer, and there is no bottom debug banner. Gameplay highlights, nametags and optional debug bounds are suppressed while a game overlay, Mod Settings, F7 menu or picker is open.

## Rendering and visibility

Highlight and nametag through-wall settings are independent. Disabling either uses the scene depth buffer: only the obstructed portions are hidden. There is no camera-to-head visibility ray and no whole-tag line-of-sight switch.

Highlights draw the actual active avatar body/clothing/head submeshes with CommandBuffer.DrawRenderer. The installed SkyCoop bundle shader is `Custom/Outline Fill`, at `assets/quickoutline/resources/shaders/outlinefill.shader`. UnityPy inspection verified `_ZTest`, `_OutlineColor`, `_OutlineWidth`, Cull Off, ZWrite Off, alpha blending, RGB-only writes and fixed NotEqual(1) stencil testing. Visible-only mode draws BeforeImageEffects with LessEqual, preserving scene depth. Through-wall mode draws AfterEverything with Always and clears depth/stencil while preserving color to avoid the fixed stencil restriction. Other mods consuming depth in a later command buffer may be affected by that existing clear.

Gameplay nametags use a single cached RGBA label texture. With through-walls enabled, GUI.DrawTexture draws that image using the same path as the settings preview; gameplay menu suppression still applies. With through-walls disabled, one camera-facing quad draws at AfterForwardAlpha, retaining the world camera's current color/depth attachments instead of forcing CameraTarget before image effects. The shader is the installed built-in `UI/Default`; inspection of `tld_Data/Resources/unity_builtin_extra` verified alpha blending, Cull Off, ZWrite Off and the named depth-state variable `unity_GUIZTestMode`. The world materials set LessEqual depth testing explicitly, disable UI clipping and leave stencil unchanged. Label placement starts at the top-center of the combined renderer bounds. Both modes share pixel placement, clamping, color, fade and range checks. ScreenToWorldPoint at the anchor depth converts pixel offset/size into camera-facing world geometry for the depth-tested mode, updating with camera FOV and distance. Occlusion is evaluated per pixel at the label's plane above the head.

Version 1.5.0 removes `NameTagOutline.cs` and repeated glyph copies. `NametagRasterizer.cs` captures the actual native font atlas using verified CoreModule Graphics.Blit, RenderTexture.GetTemporary, Texture2D.ReadPixels and GetPixels32 APIs; the font atlas itself need not be CPU-readable. UV corners and bounds from Font.GetCharacterInfo handle packed/rotated glyphs, overhangs and descenders. `AlphaMask.cs` computes the maximum glyph coverage within the stroke radius, producing a continuous outline without blending duplicate text. Foreground color and dark stroke are precomposed into a straight-alpha image. One draw applies overall opacity once, avoiding opacity buildup. Width zero preserves original glyph alpha; widths 1-3 remain configurable. The optional rectangular background is a separate draw.

Each player and the settings preview cache their own bitmap. Text/font/size/style/padding/outline changes rerasterize it; RGB changes recolor the cached coverage; distance fade and opacity changes only update draw tint. Rounded distance digits are preloaded into the font atlas. Atlas CPU snapshots are shared and refreshed only when an uncached bitmap needs a changed atlas. Existing label bitmaps remain valid after atlas repacking and do not rerasterize simply because another font rebuilt. Unchanged dimensions reuse the quad mesh. Preview and gameplay use the same texture path, with no GUI.Label outline copies. Textures, meshes and materials are released on removal/shutdown; temporary readback targets restore the previous RenderTexture.active in a finally block.

This is a cached bitmap outline rather than a signed-distance-field shader. Font-atlas readback can briefly stall rendering when the atlas changes, and newly changed text incurs CPU rasterization/dilation. Steady-state labels require no readback, rasterization or duplicate glyph geometry. A custom SDF shader would require a compatible compiled shader bundle and distance-field atlas; the inspected stock UI shader does not provide that outline facility.

Nametag font, size, style, opacity, independent RGB, outline, background, padding, vertical offset, distance text, length limit, range, nearby fade, distance scaling and screen clamping remain configurable. Names are sanitized and treated as literal text. The game's generated Font(string[], int) constructor is stripped, so installed fonts use native IL2CPP allocation followed by Font.Internal_CreateDynamicFont. The previous run's log confirmed successful native loading of the offered Windows fonts.

Version 1.5.5 removes a rendering guard that incorrectly required an explicit GUI skin font even when an installed font was selected. A null skin font can represent Unity's implicit default; the default choice now falls back to the verified native Font.GetDefault call. Font requests, atlas readback and bitmap preparation run during Update, before camera rendering; the camera callback draws prepared labels without font-atlas capture.

Version 1.5.6 separates through-wall GUI drawing from depth-tested world drawing. The user's 1.5.5 log showed a submitted world quad but no visible label, while the settings preview was visible, locating the problem after bitmap preparation. Inspection of the installed CameraGlobalRT proxy confirms separate world/weapon/image-effect cameras, a main render texture and an OnRenderImage callback. Its native implementation is not exposed by the proxy; the suspected overwrite from forcing CameraTarget before image effects remains an inference. F10 now reports GUI/world draw counts, pass timing and compositor/target texture information to verify the corrected world path in-game.

Near fade uses smoothstep(distance / Full strength distance) independently of the min/max filter. Strength scales to 65% opacity per avatar surface. The compiled fill shader draws both faces and overlapping clothing surfaces may accumulate opacity. Both distance settings support up to 1000m. Original avatar materials and layers are preserved; temporarily enabled updateWhenOffscreen flags are restored on suppression, range exit, release and shutdown.

## Verified SkyCoop structure and debug avatar

The installed assembly confirms the nested `SkyCoop.Comps+MultiplayerPlayer` component, `MyMod.players` slot list, `playersData` names/scene/loading data and `API.m_MyClientID`. Remote players are filtered for local ID, active state, scene ID/GUID and loading status; teleport revalidates the slot. The renderer root is `m_Player`, with clothing at child 0 and body at child 1. Mackenzie/Astrid head objects are body child 1's children 0/1. Head mesh transforms may share the avatar origin, so the label anchor uses active renderer bounds. Concrete SkinnedMeshRenderer and MeshRenderer queries preserve the correct IL2CPP proxy types. Discovery runs every 0.5s and hierarchy scans every 1s.

F9 instantiates the actual raw `multiplayerPlayer` prefab from the loaded SkyCoop bundle. Networking components are added separately by Shared.InitAllPlayers; the debug path does not invoke it or register a network slot. It disables scripts/colliders/extras/speaker, selects the male head, and retains default clothing and Animator. It exercises the same highlight, tag, menu and teleport paths without another client. It does not reproduce remote network animation or clothing changes. Teleport uses the validated root plus 1.5m and does not search for safe terrain. F8 remains reserved for the game's debug info.

## Verification

Version 1.5.6 builds against the installed references. There is no available in-process object debugger; assembly/shader inspection and logs provide the runtime evidence. The nametag fix needs visual verification after restarting. Enable debug tools, then test F9/F11, camera rotation/FOV changes, a wall covering half the avatar/label, both visibility toggles independently, menu/map/inventory/options layering, repeated font switches, two-line distance, zero opacity, range limits, then actual remote players. F10 records all active camera paths and depths, compositor and tag target textures, tag shader/atlas/error state, prepared/world/GUI label counts, pass timing, the last tag render frame and default font for diagnosing skipped draws and ordering problems.

The isolated regression check in `_checks/NametagRaster` executes the production alpha-mask operation and verifies widths 0-3, continuous coverage, antialias levels, zero-width identity, border clipping, overlaps without opacity buildup, source immutability and dimensions. Native font capture, readability, cached distance updates and scene-depth rendering still require in-game verification after restart.
