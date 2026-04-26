using Content.Client.Lobby.UI;
using Content.Client._Mythos.Lobby;

namespace Content.Client.Lobby.UI;

/// <summary>
/// Mythos: chargen edits that bolt onto the upstream HumanoidProfileEditor
/// without forking its main file. Currently:
///
///   * Wires MythosFeaturePicker to a species-detection predicate so it
///     knows when to inject the Eyes synthetic tab.
///   * Hides the Appearance-tab Eyes section for Mythos species (the
///     picker has been relocated into the Features tab, OV-style).
///   * Mirrors the EyeColorPicker callback onto the relocated picker so
///     edits in either control update the same Profile.Appearance.EyeColor.
///   * Mirrors UpdateEyePickers' SetData onto the relocated picker so
///     external profile changes (load saved character, sex swap, ...)
///     flow into both pickers.
///
/// The upstream picker stays wired identically -- vanilla species see no
/// behaviour change.
/// </summary>
public sealed partial class HumanoidProfileEditor
{
    /// <summary>
    /// Species IDs whose chargen flow uses the Mythos Features tab.
    /// All Mythos species inherit AppearanceHumenMythos. Could be replaced
    /// with a SpeciesPrototype flag once that exists; for now this list
    /// is the source of truth.
    /// </summary>
    private static readonly HashSet<string> MythosSpeciesIds = new()
    {
        "Humen",
        "Aasimar",
        "HalfElf",
        "Tiefling",
        "Dullahan",
        "Demihuman",
    };

    private bool IsMythosSpeciesProfile() =>
        Profile?.Species is { } sp && MythosSpeciesIds.Contains(sp);

    /// <summary>
    /// Hook the Mythos chargen plumbing onto the upstream editor. Called
    /// from the editor constructor after MythosFeatures has been
    /// initialised by SetModel().
    /// </summary>
    private void InitializeMythosChargen()
    {
        MythosFeatures.IsMythosSpecies = IsMythosSpeciesProfile;
        MythosFeatures.FeaturesRebuilt += OnMythosFeaturesRebuilt;
    }

    private void OnMythosFeaturesRebuilt()
    {
        // The Features tab gets rebuilt whenever organ data or
        // enforcement settings change. If the rebuild produced a new
        // EyesPicker (Mythos species), re-bind its callbacks to the
        // current Profile and seed it from the live data.
        if (MythosFeatures.EyesPicker is not { } picker)
            return;

        if (Profile is not null)
            picker.SetData(Profile.Appearance.EyeColor);

        picker.OnEyeColorPicked += OnMythosEyeColorPicked;
    }

    private void OnMythosEyeColorPicked(Color newColor)
    {
        if (Profile is null)
            return;
        Profile = Profile.WithCharacterAppearance(
            Profile.Appearance.WithEyeColor(newColor));
        _markingsModel.SetOrganEyeColor(Profile.Appearance.EyeColor);
        // Keep the Appearance-tab picker (when visible for non-Mythos
        // species) in sync; the upstream editor's UpdateEyePickers
        // would handle this, but a live edit doesn't go through that.
        EyeColorPicker.SetData(Profile.Appearance.EyeColor);
        ReloadProfilePreview();
    }

    /// <summary>
    /// Toggle the Appearance-tab Eyes section based on species. Mythos
    /// species hide it (eye color lives in Features → Eyes); other
    /// species keep it visible. Called from species-change paths.
    /// </summary>
    public void RefreshMythosEyesVisibility()
    {
        var mythos = IsMythosSpeciesProfile();
        EyesAppearanceSection.Visible = !mythos;
        // Refresh the relocated picker's value if it just appeared.
        if (mythos && MythosFeatures.EyesPicker is { } picker && Profile is not null)
            picker.SetData(Profile.Appearance.EyeColor);
    }

    /// <summary>
    /// Push the current eye color onto the Mythos relocated picker as
    /// well, so external profile changes (loading a saved character,
    /// switching sex, etc.) propagate to whichever picker the player is
    /// looking at.
    /// </summary>
    private void UpdateMythosEyePicker()
    {
        if (Profile is null)
            return;
        MythosFeatures.EyesPicker?.SetData(Profile.Appearance.EyeColor);
    }
}
