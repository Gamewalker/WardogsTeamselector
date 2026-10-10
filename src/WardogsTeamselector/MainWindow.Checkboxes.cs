using System.Collections.Generic;
using System.Windows.Controls;

namespace WardogsTeamselector;

public sealed partial class MainWindow
{
    private Dictionary<string, bool> checkboxPreferences = new();
    private bool checkboxPreferencesInitialized;
    private Dictionary<string, CheckBox> PersistedCheckboxes() => new()
    {
        ["ShareTeamWithGroup"] = shareTeamWithGroup, ["AutoFollow"] = groupAuto,
        ["DryRun"] = dryRun, ["GeometryCalibrated"] = calibrated,
        ["LivePreview"] = liveUpdates, ["FocusGame"] = focusGame,
        ["DrawRegion"] = drawRegion, ["AutomaticUpdates"] = automaticUpdates
    };

    private void InitializeCheckboxPreferences()
    {
        if (!smokeMode)
        {
            try { checkboxPreferences = CheckboxPreferencesStore.Load(); }
            catch { ShowError("Gespeicherte Einstellungen nicht lesbar. Einstellungen erneut speichern."); }
            try { updatePreferences = Updates.UpdatePreferences.Load(); }
            catch { updateStatus.Text = "Update-Einstellungen nicht lesbar. Update-Einstellungen neu speichern."; }
        }
        automaticUpdates.IsChecked = updatePreferences.Enabled;
        ApplyCheckboxPreferences();
        foreach (var checkbox in PersistedCheckboxes().Values)
        {
            checkbox.Checked += (_, _) => SaveCheckboxPreferences();
            checkbox.Unchecked += (_, _) => SaveCheckboxPreferences();
        }
        checkboxPreferencesInitialized = true;
        Loaded += async (_, _) =>
        {
            if (!smokeMode && groupAuto.IsChecked == true && SelectedGroup?.Status == "Approved" && startupSettingsError == null)
                await RunGroupUiAction(() => BeginGroupFollowAsync(true));
        };
    }

    private void ApplyCheckboxPreferences()
    {
        var wasLoading = loadingFields; var wasGroupLoading = groupUiLoading;
        loadingFields = true; groupUiLoading = true;
        try
        {
            foreach (var (name, checkbox) in PersistedCheckboxes())
                if (checkboxPreferences.TryGetValue(name, out var value)) checkbox.IsChecked = value;
        }
        finally { loadingFields = wasLoading; groupUiLoading = wasGroupLoading; }
        UpdateGroupControls();
    }

    private void SaveCheckboxPreferences()
    {
        if (!checkboxPreferencesInitialized || loadingFields || groupUiLoading || closing) return;
        foreach (var (name, checkbox) in PersistedCheckboxes()) checkboxPreferences[name] = checkbox.IsChecked == true;
        if (smokeMode) return;
        try { CheckboxPreferencesStore.Save(checkboxPreferences); }
        catch { ShowError("Einstellungen konnten nicht gespeichert werden."); }
        updatePreferences.Enabled = automaticUpdates.IsChecked == true;
        if (!updatePreferences.Enabled) DiscardStagedUpdate();
    }
}
