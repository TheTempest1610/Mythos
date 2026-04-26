using Content.Client.Humanoid;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Mythos.Lobby;

/// <summary>
/// Mythos: thin wrapper around <see cref="EyeColorPicker"/> for hosting
/// in the Mythos Features chargen tab. OV's chargen lists Eyes as the
/// first feature category, so this control gives the Mythos picker an
/// Eyes tab even though eye color isn't a MarkingPrototype. The wrapper
/// re-exposes <see cref="EyeColorPicker"/>'s public API so the editor
/// can wire it the same way it wires the existing Appearance-tab picker.
///
/// Built code-only (no XAML) since the inner <see cref="EyeColorPicker"/>
/// already encapsulates everything visual.
/// </summary>
public sealed class MythosEyesPicker : BoxContainer
{
    private readonly EyeColorPicker _picker;

    public MythosEyesPicker()
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        Margin = new Robust.Shared.Maths.Thickness(10);

        AddChild(new Label { Text = Loc.GetString("humanoid-profile-editor-eyes-label") });
        _picker = new EyeColorPicker();
        AddChild(_picker);
    }

    public event Action<Color>? OnEyeColorPicked
    {
        add => _picker.OnEyeColorPicked += value;
        remove => _picker.OnEyeColorPicked -= value;
    }

    public void SetData(Color color) => _picker.SetData(color);
}
