using Content.Shared.Humanoid;

namespace Content.Shared.Clothing.Mythos;

/// <summary>
/// Pure-functional resolver for Mythos clothing state names. Given a base state
/// (e.g. <c>equipped-OUTER</c>), wearer sex/species, and a predicate that reports
/// whether a candidate state exists in the target RSI, returns the most specific
/// matching state name.
/// </summary>
/// <remarks>
/// Resolution order (first hit wins):
/// <list type="number">
///   <item>{state}-{Sex}-{Species}, e.g. <c>equipped-OUTER-Female-Dwarf</c></item>
///   <item>{state}-{Species}, e.g. <c>equipped-OUTER-Dwarf</c></item>
///   <item>{state}-{Sex}, e.g. <c>equipped-OUTER-Female</c></item>
///   <item>{state}, the original (default)</item>
/// </list>
/// When the wearer's sex is <see cref="Sex.Unsexed"/>, the sex suffix is skipped
/// entirely (steps 1 and 3 are not attempted). When species is null/empty,
/// species suffix is skipped (steps 1 and 2 are not attempted).
/// </remarks>
public static class MythosClothingStateResolver
{
    /// <summary>
    /// Convert a <see cref="Sex"/> enum value to the suffix string used in RSI
    /// state names. Returns null for <see cref="Sex.Unsexed"/> to signal that
    /// the suffix should be skipped.
    /// </summary>
    public static string? SexToSuffix(Sex sex) => sex switch
    {
        Sex.Male => "Male",
        Sex.Female => "Female",
        Sex.Unsexed => null,
        _ => null,
    };

    /// <summary>
    /// Apply the four-step fallback chain to <paramref name="baseState"/>.
    /// Returns the most specific matching state name, or the original
    /// <paramref name="baseState"/> if no specific variant exists.
    /// </summary>
    /// <param name="baseState">The starting state name (e.g. <c>equipped-OUTER</c>).</param>
    /// <param name="sex">Wearer's sex; <see cref="Sex.Unsexed"/> skips the sex suffix.</param>
    /// <param name="species">Wearer's species id; null or empty skips the species suffix.</param>
    /// <param name="stateExists">Predicate that returns true if the candidate state exists in the RSI.</param>
    /// <param name="enableSpeciesFallback">If false, species suffix steps are skipped.</param>
    /// <param name="enableSexFallback">If false, sex suffix steps are skipped.</param>
    public static string Resolve(
        string baseState,
        Sex sex,
        string? species,
        Func<string, bool> stateExists,
        bool enableSpeciesFallback = true,
        bool enableSexFallback = true)
    {
        if (string.IsNullOrEmpty(baseState))
            return baseState;

        var sexSuffix = enableSexFallback ? SexToSuffix(sex) : null;
        var speciesSuffix = enableSpeciesFallback && !string.IsNullOrEmpty(species) ? species : null;

        // Step 1: {state}-{Sex}-{Species}
        if (sexSuffix != null && speciesSuffix != null)
        {
            var combined = $"{baseState}-{sexSuffix}-{speciesSuffix}";
            if (stateExists(combined))
                return combined;
        }

        // Step 2: {state}-{Species}
        if (speciesSuffix != null)
        {
            var speciesOnly = $"{baseState}-{speciesSuffix}";
            if (stateExists(speciesOnly))
                return speciesOnly;
        }

        // Step 3: {state}-{Sex}
        if (sexSuffix != null)
        {
            var sexOnly = $"{baseState}-{sexSuffix}";
            if (stateExists(sexOnly))
                return sexOnly;
        }

        // Step 4: default
        return baseState;
    }
}
