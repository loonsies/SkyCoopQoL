using System.Collections.Generic;
using System.Reflection;

namespace SkyCoopQoL;

public partial class ModEntry
{
    private static readonly FieldInfo? settingsGuiField = typeof(ModSettings.JsonModSettings).Assembly
        .GetType("ModSettings.ModSettingsMenu")?.GetField("modSettingsGUI", BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly FieldInfo? settingsTabField = typeof(ModSettings.JsonModSettings).Assembly
        .GetType("ModSettings.ModSettingsGUI")?.GetField("currentTab", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? tabSettingsField = typeof(ModSettings.JsonModSettings).Assembly
        .GetType("ModSettings.ModTab")?.GetField("modSettings", BindingFlags.Instance | BindingFlags.NonPublic);

    private bool IsOurSettingsTabActive()
    {
        if (!this.Config.IsVisible()) return false;
        // IsVisible covers every tab; check that this one is selected.
        object? gui = settingsGuiField?.GetValue(null);
        object? tab = gui != null ? settingsTabField?.GetValue(gui) : null;
        return tab != null && tabSettingsField?.GetValue(tab) is List<ModSettings.ModSettingsBase> settings && settings.Contains(this.Config);
    }
}
