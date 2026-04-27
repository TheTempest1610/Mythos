# Mythos HUD V2

This directory holds the V2 in-game HUD reskin. It registers under `ui.layout=Mythos`
and is selectable alongside the upstream `Default` and `Separated` screens.

## Directory layout

```
_Mythos/UserInterface/
├── Screens/MythosGameScreen.xaml(.cs)   # InGameScreen subclass; root layout
├── Stylesheets/                          # [CommonSheetlet] auto-discovered rules
│   ├── MythosPalette.cs                  # Color tokens + style-class names
│   ├── MythosFrameSheetlet.cs            # MythosFrame, MythosHeader, MythosDivider, MythosLocationPill
│   ├── MythosOrbSheetlet.cs              # OrbBackplate, OrbFillHp, OrbFillQi, OrbGlow
│   ├── MythosHotbarSheetlet.cs           # MythosHotbarSlot tint
│   └── MythosTabSheetlet.cs              # MenuButton font-colour states (cyan)
└── Systems/
    ├── Character/
    │   ├── CharacterUIController.cs      # Sidebar binding hook (Mythos namespace)
    │   ├── Widgets/
    │   │   ├── CharacterPanel.xaml(.cs)  # Left sidebar: portrait + equipment
    │   │   └── MythosCharacterWindow.cs  # Opened by Character tab; hosts StatsPanel
    │   └── Controls/
    │       ├── PortraitControl.xaml(.cs)
    │       └── EquipmentSlotsControl.xaml(.cs)  # 19 SlotButton instances
    ├── Stats/
    │   ├── StatsUIController.cs          # Pushes hardcoded mock data
    │   └── Widgets/
    │       ├── StatsPanel.xaml(.cs)      # NameClassBadge + 6-row stats grid
    │       └── NameClassBadge.xaml(.cs)  # Name / level / class / XP bar
    ├── Chat/Widgets/
    │   └── MythosChatBox.cs              # ChatBox subclass; no own XAML
    ├── Vitals/
    │   ├── VitalsUIController.cs
    │   └── Widgets/VitalsOrbsBar.xaml(.cs)   # HP + Qi orbs flanking the hotbar
    └── Location/
        ├── LocationUIController.cs
        └── Widgets/LocationTimeWidget.xaml(.cs)  # Top-right time/region pill
```

## Activating the V2 HUD

In the client console:

```
cvar ui.layout Mythos
```

Then re-enter the gameplay state (reconnect, change map, or reload). Toggle the legacy
HUD root with the existing `hide_old_ui` console command.

## Mock data sources

All values shown in the HUD come from the Mythos UIControllers, not from ECS:

| Source | Values |
|--------|--------|
| `VitalsUIController` | HP 1284 / 1284, Qi 756 / 812 |
| `StatsUIController` | Name "Liu Xianyi", Lv. 12 Sword Sect Initiate, XP 4720/8000, HP 1284, Qi 756, Atk 142, Def 88, Spirit 211, Dex 167 |
| `LocationUIController` | "23:42" / "Peach Blossom Valley" / ☽ |

Real ECS subscriptions land post-mockup; the controllers are the seam where that
wiring goes.

## Placeholder texture inventory

The mockup uses `StyleBoxFlat` rectangles instead of textured 9-slice frames in this
phase. No new textures committed yet. **Before merge** the following directories must
be populated (paths are reservations, not yet present):

- `Resources/Textures/_Mythos/UI/frame/`     (9-slice MythosFrame edges + corners)
- `Resources/Textures/_Mythos/UI/orbs/`      (HP + Qi orb backplates and fills)
- `Resources/Textures/_Mythos/UI/slots/`     (equipment + hotbar slot frames)
- `Resources/Textures/_Mythos/UI/portraits/` (rendered avatar PNGs)
- `Resources/Textures/_Mythos/UI/icons/`     (stat icons)
- `Resources/Textures/_Mythos/UI/decor/`     (pagoda corners, optional)

Existing `Resources/Textures/UI/Panel_MM*.png`, `Chat_BG.png`, and `EquipmentBG.png`
(from the ui-overhaul commit) are reusable as 9-slice sources.

## Upstream touch points (`// Mythos:` tagged)

Four single-line edits make the framework wire-up:

1. `Content.Client/UserInterface/Screens/ScreenType.cs` — adds `Mythos` enum value.
2. `Content.Client/Gameplay/GameplayState.cs` — switch case routing to `MythosGameScreen`.
3. `Content.Client/UserInterface/Systems/Character/Windows/CharacterWindow.xaml.cs` — drops `sealed` so `MythosCharacterWindow` can subclass.
4. `Content.Client/UserInterface/Systems/Character/CharacterUIController.cs` — `EnsureWindow()` selects `MythosCharacterWindow` when `MythosGameScreen` is active; `CharacterWindow` otherwise.

All other Mythos code lives under `Content.Client/_Mythos/UserInterface/`.

## Testing

Phase tests live under `Content.IntegrationTests/Tests/UserInterface/`:

- `MythosGameScreenTest` - Phase 1: screen mounts + OldHud toggle
- `MythosHudPhase2Test`  - Phase 2: vitals + location + stylesheets
- `MythosHudPhase3Test`  - Phase 3: character sidebar (19 slots) + stats panel + window swap
- `MythosHudPhase4Test`  - Phase 4: chat subclass + tab restyle

Run them in batch:

```
dotnet test Content.IntegrationTests --filter "FullyQualifiedName~MythosHud|FullyQualifiedName~MythosGameScreen"
```

The fixture pool keeps the client/server pair hot between cases (~30 ms per test
after the first ~20 s setup).

## Layout gotchas (lessons from the V2 mockup pass)

Each of these failed silently in the test suite first, then bit on real-game launch.
Documented so they aren't re-discovered.

### 1. `HorizontalAlignment="Center"` on a child of a vertical `BoxContainer` does not center the child

`Control.ArrangeCore` has the centering math (`origin.X += (avail.X - size.X) / 2`),
but for reasons we couldn't pin down it didn't apply when a `BoxContainer Vertical`
arranged the child via `LayOutItems`. Position stayed at `(0, 0)` despite
`HAlign=Center` and `Size=(556, 80)` inside a `(1280, 80)` rect.

Use the spacer pattern instead:

```xml
<BoxContainer Orientation="Horizontal" HorizontalExpand="True">
    <Control HorizontalExpand="True" />
    <BoxContainer Name="Content"> ... actual children ... </BoxContainer>
    <Control HorizontalExpand="True" />
</BoxContainer>
```

The two flex `Control`s expand symmetrically and push `Content` to the centre. This
is the pattern `VitalsOrbsBar` uses.

### 2. `SetAnchorPreset(BottomWide) + SetMargin*` alone renders off-screen

`BottomWide` sets `AnchorTop = 1` (parent's bottom edge). With `MarginTop = 0` the
widget's top edge is AT `parent.bottom`, and `GrowDirection.End` then grows the
widget DOWNWARD off-screen. Use `SetAnchorAndMarginPreset(..., margin: 0)`:
its measure pass computes `MarginTop = -widgetHeight` so the widget sits ABOVE the
bottom anchor.

### 3. Layout tests must mirror `UIManager.LoadScreenInternal`'s alignment override

`LoadScreenInternal` (RobustToolbox) forces `screen.HorizontalAlignment = Stretch`
and `VerticalAlignment = Stretch` AFTER constructing the screen. If a test just
calls `screen.Arrange(rect)`, the screen renders at its DesiredSize (the largest
single child's measured size, e.g. 556x80 for the bottom strip) and centres itself
within the arrange bounds, masking horizontal-centering bugs.

`MythosHudLayoutTest.BuildLaidOutScreen` mirrors the override — replicate it for
any new screen-level layout tests.

### 4. `SetAnchorAndMarginPreset` measures before live data populates labels

The preset's measure pass uses `control.DesiredSize` AS-OF construction time. If a
`UIController` populates labels after the screen loads (e.g. `LocationUIController.SetLocation`
fires from `OnStateEntered`), the measured width is wrong and the widget is
truncated.

Two fixes that work together: set realistic default text directly in XAML, and set
a `MinSize` on the widget root that's large enough for the live values.

### 5. Layout assertions must check the widget is ON-SCREEN

The first iteration of `BottomStrip_OrbsAreSnugAroundHotbar` asserted
`hpRect.Bottom > screenHeight * 0.85`. An off-screen widget at `y = 720..800`
satisfies that. Always assert against the parent's own bounds (e.g.
`hpRect.Bottom <= hudRect.Bottom + 1`) so an off-screen widget can't pass.

### 6. SS14 sandbox `ILVerify` gotcha

See the project-wide note in `CLAUDE.md` — `stackalloc Span<T>` is unverifiable in
content assemblies. The orb's custom `OrbControl.Draw` uses `new Vector2[]` rather
than `stackalloc` for this reason.
