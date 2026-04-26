using System.Collections.Generic;

namespace Content.Shared.Humanoid.Markings;

// Mythos: with-style helpers for the per-instance Mythos fields on
// Marking. The field declarations themselves live in Marking.cs because
// CS0282 forbids splitting struct fields across partial declarations
// (would leave the layout / serialization order undefined).
public partial record struct Marking
{
    public Marking WithMythosSize(int? size) =>
        this with { MythosSizeIndex = size };

    public Marking WithMythosToggle(string name, bool value)
    {
        var toggles = MythosToggles is null
            ? new Dictionary<string, bool>()
            : new Dictionary<string, bool>(MythosToggles);
        toggles[name] = value;
        return this with { MythosToggles = toggles };
    }

    public Marking WithMythosVariant(string? variant) =>
        this with { MythosVariant = variant };
}
