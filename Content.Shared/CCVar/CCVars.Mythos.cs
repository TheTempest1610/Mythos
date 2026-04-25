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
}
