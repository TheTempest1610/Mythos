using Robust.Shared.Maths;

namespace Content.Client._Mythos.UserInterface.Stylesheets;

// Mythos: Color tokens for the V2 HUD reskin. Mirrors MythosThemeSheetlet's main-menu
// palette so the in-game HUD and main menu read as the same brand.
// Source of truth: any future palette changes happen here.
public static class MythosPalette
{
    public static readonly Color MoonWhite     = Color.FromHex("#F2F6FF");
    public static readonly Color Mist          = Color.FromHex("#B7C6D9");
    public static readonly Color Accent        = Color.FromHex("#5AD8FF");
    public static readonly Color AccentStrong  = Color.FromHex("#8CE7FF");
    public static readonly Color AccentDim     = Color.FromHex("#27495F");
    public static readonly Color Ink           = Color.FromHex("#0D131C");
    public static readonly Color Surface       = Color.FromHex("#161E29");
    public static readonly Color SurfaceRaised = Color.FromHex("#1E2A38");
    public static readonly Color SurfaceMuted  = Color.FromHex("#223243");
    public static readonly Color Edge          = Color.FromHex("#33556D");
    public static readonly Color EdgeBright    = Color.FromHex("#67CFEF");
    public static readonly Color Danger        = Color.FromHex("#5A2D34");
    public static readonly Color DangerHover   = Color.FromHex("#6B3640");
    public static readonly Color DangerPressed = Color.FromHex("#4B242A");

    // Orb fills
    public static readonly Color HpFill = Accent;            // Cyan
    public static readonly Color QiFill = MoonWhite;         // Moon-white

    public const string FrameClass         = "MythosFrame";
    public const string HeaderClass        = "MythosHeader";
    public const string DividerClass       = "MythosDivider";
    public const string OrbBackplateClass  = "MythosOrbBackplate";
    public const string OrbFillHpClass     = "MythosOrbFillHp";
    public const string OrbFillQiClass     = "MythosOrbFillQi";
    public const string OrbGlowClass       = "MythosOrbGlow";
    public const string HotbarSlotClass    = "MythosHotbarSlot";
    public const string LocationPillClass  = "MythosLocationPill";
}
