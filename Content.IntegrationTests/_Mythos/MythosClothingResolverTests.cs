#nullable enable
using System.Collections.Generic;
using Content.Shared.Clothing.Mythos;
using Content.Shared.Humanoid;

namespace Content.IntegrationTests._Mythos;

/// <summary>
/// Unit tests for the Mythos clothing state-name resolver. These exercise the
/// pure-functional <see cref="MythosClothingStateResolver"/> that the client
/// system delegates to, the predicate-based design lets us hand-build the
/// "what states exist in the RSI" set without spinning up a game pair.
/// </summary>
[TestFixture]
[TestOf(typeof(MythosClothingStateResolver))]
public sealed class MythosClothingResolverTests
{
    /// <summary>Build a state-existence predicate from a fixed set of state names.</summary>
    private static System.Func<string, bool> States(params string[] names)
    {
        var set = new HashSet<string>(names);
        return set.Contains;
    }

    // ----- Plan-mandated fallback chain cases (7 of them) -----

    [Test]
    public void HumanMale_DefaultOnly_ResolvesToDefault()
    {
        // Wearer Human Male wearing item with `equipped-OUTER` only
        // -> resolves to `equipped-OUTER`
        var result = MythosClothingStateResolver.Resolve(
            "equipped-OUTER",
            Sex.Male,
            "Human",
            States("equipped-OUTER"));

        Assert.That(result, Is.EqualTo("equipped-OUTER"));
    }

    [Test]
    public void HumanFemale_FemaleStateOnly_ResolvesToFemale()
    {
        // Wearer Human Female + item with `equipped-OUTER-Female` only
        // -> resolves to `equipped-OUTER-Female`
        var result = MythosClothingStateResolver.Resolve(
            "equipped-OUTER",
            Sex.Female,
            "Human",
            States("equipped-OUTER", "equipped-OUTER-Female"));

        Assert.That(result, Is.EqualTo("equipped-OUTER-Female"));
    }

    [Test]
    public void DwarfMale_DwarfStateOnly_ResolvesToDwarf()
    {
        // Wearer Dwarf Male + item with `equipped-OUTER-Dwarf` only
        // -> resolves to `equipped-OUTER-Dwarf`
        var result = MythosClothingStateResolver.Resolve(
            "equipped-OUTER",
            Sex.Male,
            "Dwarf",
            States("equipped-OUTER", "equipped-OUTER-Dwarf"));

        Assert.That(result, Is.EqualTo("equipped-OUTER-Dwarf"));
    }

    [Test]
    public void DwarfFemale_CombinedStateExists_ResolvesToCombined()
    {
        // Wearer Dwarf Female + item with `equipped-OUTER-Female-Dwarf`
        // -> resolves to combined state
        var result = MythosClothingStateResolver.Resolve(
            "equipped-OUTER",
            Sex.Female,
            "Dwarf",
            States(
                "equipped-OUTER",
                "equipped-OUTER-Female",
                "equipped-OUTER-Dwarf",
                "equipped-OUTER-Female-Dwarf"));

        Assert.That(result, Is.EqualTo("equipped-OUTER-Female-Dwarf"));
    }

    [Test]
    public void DwarfFemale_NoCombinedButBothSeparate_SpeciesWins()
    {
        // Wearer Dwarf Female + item with both `-Female` and `-Dwarf` (no combined)
        // -> species wins (`-Dwarf`)
        // Documented priority: species suffix is checked at step 2, before sex
        // suffix at step 3, so species variant beats sex variant when both exist.
        var result = MythosClothingStateResolver.Resolve(
            "equipped-OUTER",
            Sex.Female,
            "Dwarf",
            States(
                "equipped-OUTER",
                "equipped-OUTER-Female",
                "equipped-OUTER-Dwarf"));

        Assert.That(result, Is.EqualTo("equipped-OUTER-Dwarf"));
    }

    [Test]
    public void HumanFemale_NoFemaleState_FallsThroughToDefault()
    {
        // Wearer Human Female + item with no female state
        // -> falls through to `equipped-OUTER`
        var result = MythosClothingStateResolver.Resolve(
            "equipped-OUTER",
            Sex.Female,
            "Human",
            States("equipped-OUTER"));

        Assert.That(result, Is.EqualTo("equipped-OUTER"));
    }

    [Test]
    public void Unsexed_SkipsSexSuffixEntirely()
    {
        // Wearer with `Sex.Unsexed`
        // -> suffix-skipped (only species suffix is considered)
        var result = MythosClothingStateResolver.Resolve(
            "equipped-OUTER",
            Sex.Unsexed,
            "Dwarf",
            States(
                "equipped-OUTER",
                "equipped-OUTER-Dwarf",
                // These should NOT be picked: there is no Unsexed sex suffix.
                "equipped-OUTER-Male",
                "equipped-OUTER-Female"));

        Assert.That(result, Is.EqualTo("equipped-OUTER-Dwarf"));
    }

    // ----- Backward-compat regression -----
    // An existing pre-V1 SS14 clothing item with no sex/species variants in its
    // RSI must resolve identically to before. We mimic that by giving the
    // resolver an RSI containing only the base state, the resolver must return
    // the base state unchanged across every (sex, species) combination.
    [TestCase(Sex.Male, "Human")]
    [TestCase(Sex.Female, "Human")]
    [TestCase(Sex.Male, "Dwarf")]
    [TestCase(Sex.Female, "Dwarf")]
    [TestCase(Sex.Unsexed, "Reptilian")]
    [TestCase(Sex.Male, null)]
    [TestCase(Sex.Female, null)]
    [TestCase(Sex.Unsexed, null)]
    public void BackwardCompat_PreV1ItemWithNoVariants_ResolvesToDefault(Sex sex, string? species)
    {
        // No -Sex / -Species states exist; resolver must return the base state.
        var result = MythosClothingStateResolver.Resolve(
            "equipped-OUTER",
            sex,
            species,
            States("equipped-OUTER"));

        Assert.That(result, Is.EqualTo("equipped-OUTER"));
    }

    // ----- Additional coverage for edge cases -----

    [Test]
    public void EmptyBaseState_ReturnsAsIs()
    {
        var result = MythosClothingStateResolver.Resolve(
            string.Empty,
            Sex.Female,
            "Human",
            _ => true);

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void NullSpecies_OnlySexFallbackAttempted()
    {
        // species = null means species suffix is skipped; sex still applies.
        var result = MythosClothingStateResolver.Resolve(
            "equipped-OUTER",
            Sex.Female,
            null,
            States("equipped-OUTER", "equipped-OUTER-Female"));

        Assert.That(result, Is.EqualTo("equipped-OUTER-Female"));
    }

    [Test]
    public void SpeciesFallbackDisabled_OnlySexConsidered()
    {
        // EnableSpeciesFallback = false => steps 1 and 2 are skipped.
        var result = MythosClothingStateResolver.Resolve(
            "equipped-OUTER",
            Sex.Female,
            "Dwarf",
            States(
                "equipped-OUTER",
                "equipped-OUTER-Female",
                "equipped-OUTER-Dwarf",
                "equipped-OUTER-Female-Dwarf"),
            enableSpeciesFallback: false);

        Assert.That(result, Is.EqualTo("equipped-OUTER-Female"));
    }

    [Test]
    public void SexFallbackDisabled_OnlySpeciesConsidered()
    {
        // EnableSexFallback = false => steps 1 and 3 are skipped.
        var result = MythosClothingStateResolver.Resolve(
            "equipped-OUTER",
            Sex.Female,
            "Dwarf",
            States(
                "equipped-OUTER",
                "equipped-OUTER-Female",
                "equipped-OUTER-Dwarf",
                "equipped-OUTER-Female-Dwarf"),
            enableSexFallback: false);

        Assert.That(result, Is.EqualTo("equipped-OUTER-Dwarf"));
    }

    [Test]
    public void SexToSuffix_MapsCorrectly()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MythosClothingStateResolver.SexToSuffix(Sex.Male), Is.EqualTo("Male"));
            Assert.That(MythosClothingStateResolver.SexToSuffix(Sex.Female), Is.EqualTo("Female"));
            Assert.That(MythosClothingStateResolver.SexToSuffix(Sex.Unsexed), Is.Null);
        });
    }
}
