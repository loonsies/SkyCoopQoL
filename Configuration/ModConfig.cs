using ModSettings;
using UnityEngine;

namespace SkyCoopQoL.Framework;

public class ModConfig : JsonModSettings
{
    [Section("Player Highlight")]
    [Name("Enabled")]
    [Description("Enable player highlighting while in SkyCoop games.")]
    public bool Enabled = true;

    [Name("Show menu key")]
    [Description("Toggle the list of nearby players and teleport menu.")]
    public KeyCode ToggleMenu = KeyCode.F7;

    [Name("Highlight through walls")]
    [Description("Render player highlights over walls. Disable to use the scene depth buffer and highlight only visible surfaces.")]
    public bool HighlightThroughWalls = true;

    [Name("Limit highlight distance")]
    [Description("Apply the min/max distance filter to highlights. The player list remains available outside this range.")]
    public bool LimitDistance = true;

    [Name("Min highlight distance")]
    [Description("Players closer than this are not highlighted.")]
    [Slider(0, 100, 0)]
    public int MinDistance = 20;

    [Name("Max highlight distance")]
    [Description("Players farther away than this are not highlighted.")]
    [Slider(10, 1000, 991)]
    public int MaxDistance = 300;

    [Name("Highlight strength")]
    [Description("Scale ghost opacity. Maximum per-surface opacity is 65% to retain translucency; overlapping surfaces can appear stronger.")]
    [Slider(10, 100, 70)]
    public int HighlightStrength = 70;

    [Name("Fade highlight nearby")]
    [Description("Reduce ghost opacity as you approach the player.")]
    public bool FadeWhenNearby = true;

    [Name("Full strength distance")]
    [Description("Ghost opacity smoothly rises from zero at the player to full strength at this distance in meters.")]
    [Slider(1, 1000, 1000)]
    public int NearFadeDistance = 150;

    [Name("Red")]
    [Description("Highlight red channel (0-255).")]
    [Slider(0, 255, 256)]
    public int HighlightRed = 56;

    [Name("Green")]
    [Description("Highlight green channel (0-255).")]
    [Slider(0, 255, 256)]
    public int HighlightGreen = 209;

    [Name("Blue")]
    [Description("Highlight blue channel (0-255).")]
    [Slider(0, 255, 256)]
    public int HighlightBlue = 255;

    [Section("Player Nametags")]
    [Name("Show nametags")]
    [Description("Show names above remote players and the local test model, including through walls.")]
    public bool ShowNameTags = true;

    [Name("Nametags through walls")]
    [Description("Show nametags when the player's head is behind world geometry. Disable to let world geometry occlude individual parts of the label using scene depth.")]
    public bool NameTagsThroughWalls = false;

    [Name("Font")]
    [Description("Use the default GUI font or an installed Windows font. Unavailable fonts fall back to the default.")]
    [Choice("Default GUI", "Arial", "Segoe UI", "Consolas", "Tahoma")]
    public int NameTagFont = 0;

    [Name("Font size")]
    [Description("Nametag font size in screen pixels.")]
    [Slider(8, 48, 41)]
    public int NameTagFontSize = 16;

    [Name("Font style")]
    [Choice("Normal", "Bold", "Italic", "Bold italic")]
    public int NameTagFontStyle = 1;

    [Name("Nametag opacity")]
    [Description("Overall opacity of text, outline and background.")]
    [Slider(0, 100, 101)]
    public int NameTagOpacity = 90;

    [Name("Nametag red")]
    [Slider(0, 255, 256)]
    public int NameTagRed = 255;

    [Name("Nametag green")]
    [Slider(0, 255, 256)]
    public int NameTagGreen = 255;

    [Name("Nametag blue")]
    [Slider(0, 255, 256)]
    public int NameTagBlue = 255;

    [Name("Text outline width")]
    [Description("Dark outline in pixels. Set to zero to disable.")]
    [Slider(0, 3, 4)]
    public int NameTagOutlineWidth = 1;

    [Name("Background opacity")]
    [Description("Opacity of the dark label background, multiplied by overall nametag opacity. Zero disables it.")]
    [Slider(0, 100, 101)]
    public int NameTagBackgroundOpacity = 25;

    [Name("Background padding")]
    [Slider(0, 16, 17)]
    public int NameTagPadding = 5;

    [Name("Height above player")]
    [Description("Pixel gap between the top of the player bounds and the bottom of the nametag.")]
    [Slider(0, 80, 81)]
    public int NameTagOffset = 12;

    [Name("Show distance")]
    public bool NameTagShowDistance = true;

    [Name("Distance on separate line")]
    public bool NameTagDistanceOnNewLine = false;

    [Name("Maximum name length")]
    [Description("Truncate long player names to keep labels readable.")]
    [Slider(8, 64, 57)]
    public int NameTagMaxNameLength = 32;

    [Name("Follow highlight distance filter")]
    [Description("Hide nametags outside the configured min/max highlight range when that filter is enabled.")]
    public bool NameTagFollowHighlightRange = true;

    [Name("Max nametag distance")]
    [Description("Independent maximum distance for nametags, in meters.")]
    [Slider(1, 1000, 1000)]
    public int NameTagMaxDistance = 1000;

    [Name("Fade nametags nearby")]
    [Description("Apply the highlight's near-distance fade to nametags, in addition to their own opacity.")]
    public bool NameTagFadeNearby = false;

    [Name("Scale text with distance")]
    [Description("Shrink text gradually toward the minimum size at the max nametag distance. Disabled keeps a fixed screen size.")]
    public bool NameTagScaleWithDistance = false;

    [Name("Minimum scaled font size")]
    [Slider(8, 48, 41)]
    public int NameTagMinFontSize = 10;

    [Name("Keep labels within screen")]
    [Description("Keep labels attached to visible anchors inside the screen edges. Players behind the camera remain hidden.")]
    public bool NameTagClampToScreen = true;

    [Section("Settings Previews")]
    [Name("Show highlight preview")]
    public bool ShowHighlightPreview = true;

    [Name("Show nametag preview")]
    public bool ShowNameTagPreview = true;

    [Name("Preview height above bottom")]
    [Description("Pixel gap below the preview windows. Increase to keep them above the game's bottom buttons.")]
    [Slider(100, 360, 261)]
    public int PreviewBottomMargin = 170;

    [Section("Debug Tools")]
    [Name("Enable debug tools")]
    [Description("Enable the test player, diagnostic keys and optional bounds display.")]
    public bool DebugMode = false;

    [Name("Spawn/remove debug ghost key")]
    [Description("Spawn or remove a local test player. No multiplayer session needed.")]
    public KeyCode ToggleDebug = KeyCode.F9;

    [Name("Toggle debug motion key")]
    [Description("Move the debug ghost sideways to verify projection and range filtering.")]
    public KeyCode ToggleDebugMotion = KeyCode.F11;

    [Name("Dump player hierarchy key")]
    [Description("Log player and rendering details. Hold Shift for the full hierarchy.")]
    public KeyCode DumpPlayers = KeyCode.F10;

    [Name("Show debug bounds box")]
    [Description("Show projected renderer bounds while the debug player is spawned. This box is diagnostic only.")]
    public bool ShowDebugBounds = false;

    [Name("Debug spawn distance")]
    [Description("Distance in meters in front of you when spawning the test ghost. Respawn after changing this setting.")]
    [Slider(2, 200, 12)]
    public int DebugDistance = 12;

    private bool? lastDebugVisibility;

    internal void UpdateDebugVisibility()
    {
        if (this.lastDebugVisibility == this.DebugMode) return;
        this.SetFieldVisible(nameof(this.ToggleDebug), this.DebugMode);
        this.SetFieldVisible(nameof(this.ToggleDebugMotion), this.DebugMode);
        this.SetFieldVisible(nameof(this.DumpPlayers), this.DebugMode);
        this.SetFieldVisible(nameof(this.ShowDebugBounds), this.DebugMode);
        this.SetFieldVisible(nameof(this.DebugDistance), this.DebugMode);
        this.lastDebugVisibility = this.DebugMode;
    }

}
