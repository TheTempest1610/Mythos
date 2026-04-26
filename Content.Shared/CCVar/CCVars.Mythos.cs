using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

// Mythos: CVars for Mythos-specific runtime feature gates.
public sealed partial class CCVars
{
    /// <summary>
    ///     Mythos: master switch for atmospheric simulation. When false, AtmosphereSystem,
    ///     BarotraumaSystem, FlammableSystem, RespiratorSystem, TemperatureSystem, and
    ///     ThermalRegulatorSystem all skip their per-tick Update. Upstream prototypes
    ///     keep their atmos components untouched; the systems just no-op.
    /// </summary>
    public static readonly CVarDef<bool> MythosAtmosEnabled =
        CVarDef.Create("mythos.atmos.enabled", true, CVar.SERVERONLY);

    /// <summary>
    ///     Mythos: gate for adult-only chargen feature categories
    ///     (Penis, Vagina, Testicles, Breasts, Belly, Butt). When false,
    ///     these tabs are hidden in the Features chargen panel and any
    ///     existing markings the player has on those layers are still
    ///     respected by the renderer; new selection is just impossible.
    ///     Default false so vanilla / public servers stay SFW. Mythos
    ///     dev preset overrides this to true so we can test.
    ///     Replicated to clients so the chargen UI knows to hide tabs.
    /// </summary>
    public static readonly CVarDef<bool> MythosChargenErpFeatures =
        CVarDef.Create("mythos.chargen_erp_features", false, CVar.SERVER | CVar.REPLICATED);
}
