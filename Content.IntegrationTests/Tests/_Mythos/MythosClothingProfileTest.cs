// Mythos: regression test for the chargen "Clothing" tab persistence
// path. Pins three contracts that the wiring relies on:
//
//   (1) HumanoidCharacterProfile copy-ctor preserves
//       MythosClothingSelections. Every With* call funnels through the
//       upstream copy constructor, which doesn't touch partial-class
//       fields by default. We added a partial-method hook
//       (CopyMythosFieldsFrom) so the field survives. Without it, a
//       single WithName/WithSex/etc. would silently reset clothing
//       selections to an empty dict and the player would lose their
//       picks on the next chargen edit.
//
//   (2) WithMythosClothing produces a profile with an independent
//       dictionary copy. If we returned a shared ref, later
//       WithMythosClothing calls (or external mutations) would mutate
//       the original.
//
//   (3) MemberwiseEquals participates in MythosClothingSelections.
//       The chargen editor's SetDirty bookkeeping calls MemberwiseEquals
//       on the live profile vs. the stored character to decide whether
//       to enable the Save button. If MythosClothingSelections were
//       ignored, edits in the Clothing tab wouldn't enable Save.
//
// These are pure data tests -- no server / prototype manager needed --
// so they run in-process without spinning up a TestPair.
using System.Collections.Generic;
using Content.Shared.Preferences;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Mythos;

[TestOf(typeof(HumanoidCharacterProfile))]
public sealed class MythosClothingProfileTest
{
    private static Dictionary<string, EntProtoId> SamplePicks() => new()
    {
        ["head"] = "OVSteelHalfPlate",
        ["outerClothing"] = "OVChainmail",
        ["shoes"] = "OVLeatherBoots",
    };

    [Test]
    public void WithMythosClothing_PreservesSelectionsAcrossWithCalls()
    {
        var picks = SamplePicks();
        var profile = new HumanoidCharacterProfile()
            .WithMythosClothing(picks)
            // Round-trip through unrelated With calls to exercise the
            // copy-constructor partial-method hook.
            .WithName("Test")
            .WithFlavorText("ignored")
            .WithAge(30);

        Assert.That(profile.MythosClothingSelections,
            Is.EquivalentTo(picks));
    }

    [Test]
    public void WithMythosClothing_DefensivelyCopiesIncomingMap()
    {
        var picks = SamplePicks();
        var profile = new HumanoidCharacterProfile().WithMythosClothing(picks);

        // Mutating the source dict after the call must not bleed into
        // the stored selections.
        picks["head"] = "WrongPrototype";
        picks["belt"] = "OVLeatherBelt";

        Assert.That(profile.MythosClothingSelections["head"].Id,
            Is.EqualTo("OVSteelHalfPlate"));
        Assert.That(profile.MythosClothingSelections.ContainsKey("belt"),
            Is.False);
    }

    [Test]
    public void MemberwiseEquals_DiffersOnMythosClothingChange()
    {
        var a = new HumanoidCharacterProfile().WithMythosClothing(SamplePicks());
        var b = new HumanoidCharacterProfile().WithMythosClothing(
            new Dictionary<string, EntProtoId>
            {
                ["head"] = "OVSteelHalfPlate",
                // outerClothing differs
                ["outerClothing"] = "OVPaddedJacket",
                ["shoes"] = "OVLeatherBoots",
            });

        Assert.That(a.MemberwiseEquals(b), Is.False,
            "Differing MythosClothingSelections must flag the profile "
            + "as changed; otherwise the chargen Save button never "
            + "enables after a clothing-tab edit.");

        var c = new HumanoidCharacterProfile().WithMythosClothing(SamplePicks());
        Assert.That(a.MemberwiseEquals(c), Is.True,
            "Equivalent selections must round-trip equal; otherwise the "
            + "Save button stays enabled after a no-op edit.");
    }

    [Test]
    public void CopyConstructor_TakesIndependentDictionary()
    {
        var original = new HumanoidCharacterProfile().WithMythosClothing(SamplePicks());
        var copy = new HumanoidCharacterProfile(original);

        // Mutate the copy's selections via WithMythosClothing (returns a
        // fresh profile but reuses the underlying dict identity check
        // would be brittle). We assert via MemberwiseEquals + a probe.
        Assert.That(copy.MythosClothingSelections["head"].Id,
            Is.EqualTo("OVSteelHalfPlate"));

        // Reach in and assert reference inequality so a future refactor
        // that "optimises" by sharing the dict trips this guard.
        Assert.That(
            ReferenceEquals(
                original.MythosClothingSelections,
                copy.MythosClothingSelections),
            Is.False,
            "Copy must not share its MythosClothingSelections dict with "
            + "the source; future mutations would otherwise leak.");
    }
}
