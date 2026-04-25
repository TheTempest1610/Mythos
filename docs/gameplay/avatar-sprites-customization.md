# Avatar Sprites and Customization

## Purpose

This page explains how visible entities, humanoid bodies, clothing, tails, ears, hair, snouts, markings, and Mythos fantasy avatar assets are assembled. Use it before adding ancestries, body parts, imported Roguetown clothing, player customization options, or mob sprites.

The important rule: humanoid visuals are a layered `SpriteComponent`. Body organs, markings, and clothing do not attach as child sprites. They write or insert layers into the same wearer sprite stack.

Official SS14 references:

- Sprites and icons: https://docs.spacestation14.com/en/robust-toolbox/rendering/sprites-and-icons.html
- RSI format: https://docs.spacestation14.com/en/specifications/robust-station-image.html
- Dynamic sprites and visualizers: https://docs.spacestation14.com/en/ss14-by-example/making-a-sprite-dynamic.html

## Source Anchors

- `RobustToolbox/Robust.Shared/GameObjects/Components/Renderable/SpriteLayerData.cs`
- `RobustToolbox/Robust.Shared/Utility/SpriteSpecifier.cs`
- `RobustToolbox/Robust.Client/ResourceManagement/ResourceTypes/RSIResource.cs`
- `RobustToolbox/Robust.Client/Graphics/RSI/RSI.cs`
- `RobustToolbox/Robust.Client/GameObjects/EntitySystems/SpriteSystem.LayerMap.cs`
- `Content.Shared/Humanoid/HumanoidVisualLayers.cs`
- `Content.Shared/Humanoid/HumanoidVisualLayersExtension.cs`
- `Content.Shared/Humanoid/HumanoidCharacterAppearance.cs`
- `Content.Shared/Preferences/HumanoidCharacterProfile.cs`
- `Content.Shared/Humanoid/Markings/MarkingPrototype.cs`
- `Content.Shared/Humanoid/Markings/MarkingManager.cs`
- `Content.Shared/Body/VisualOrganComponent.cs`
- `Content.Shared/Body/VisualOrganMarkingsComponent.cs`
- `Content.Shared/Body/SharedVisualBodySystem.cs`
- `Content.Client/Body/VisualBodySystem.cs`
- `Content.Shared/Clothing/Components/ClothingComponent.cs`
- `Content.Shared/Clothing/Components/HideLayerClothingComponent.cs`
- `Content.Shared/Clothing/EntitySystems/ClothingSystem.cs`
- `Content.Client/Clothing/ClientClothingSystem.cs`
- `Content.Shared/Clothing/EntitySystems/HideLayerClothingSystem.cs`
- `Content.Client/Humanoid/HideableHumanoidLayersSystem.cs`
- `Resources/Prototypes/Body/species_appearance.yml`
- `Resources/Prototypes/Body/base_organs.yml`
- `Resources/Prototypes/Body/Species/human.yml`
- `Resources/Prototypes/InventoryTemplates/human_inventory_template.yml`
- `Resources/Prototypes/Entities/Mobs/Customization/Markings/`
- `Resources/Prototypes/Entities/Clothing/`
- `Resources/Textures/Mobs/Species/`
- `Resources/Textures/Mobs/Customization/`
- `Resources/Textures/_Mythos/_OV/Roguetown/Clothing/`

## RSI Files

An RSI is a folder ending in `.rsi`. It contains `meta.json` plus PNG files named after RSI states. A prototype or layer references an RSI by folder path and one state name.

Example body RSI:

```text
Resources/Textures/Mobs/Species/Human/parts.rsi/
  meta.json
  torso_m.png
  torso_f.png
  head_m.png
  head_f.png
  l_arm.png
  r_arm.png
```

Example `meta.json` shape:

```json
{
  "version": 1,
  "size": { "x": 32, "y": 32 },
  "license": "CC-BY-SA-3.0",
  "copyright": "...",
  "states": [
    { "name": "head_m", "directions": 4 },
    { "name": "tail_cat", "directions": 4 }
  ]
}
```

Runtime interpretation:

- `size` is the frame size the engine crops from each PNG. It is not necessarily the PNG size.
- Each `state` normally has a matching `<state>.png`.
- `directions` can be `1`, `4`, or `8`. Four-direction states are ordered south, north, east, west in the sheet.
- `delays` is optional animation timing in seconds. It is a list per direction.
- `RSIResource` loads `meta.json`, loads the state PNGs, builds an atlas, and registers an `RSI` with state IDs.
- `SpriteSpecifier.Rsi` stores `{ sprite: <rsi path>, state: <state name> }`.
- `PrototypeLayerData` is the YAML layer data object. It can set `sprite`, `state`, `texture`, `shader`, `scale`, `rotation`, `offset`, `visible`, `color`, `map`, and animation flags.

RSI reference examples:

```yaml
- type: Sprite
  sprite: Mobs/Species/Human/parts.rsi
  state: torso_m
```

```yaml
sprites:
- sprite: Mobs/Customization/ears.rsi
  state: long_ears_standard
```

## SpriteComponent Layers

`SpriteComponent` draws layers in list order. Later layers draw over earlier layers. A layer can be addressed by numeric index, but game systems normally use a layer map key.

Layer map keys can be enum keys or strings:

```yaml
- type: Sprite
  sprite: Mobs/Species/Human/parts.rsi
  layers:
  - state: torso_m
    map: [ "enum.HumanoidVisualLayers.Chest" ]
  - state: head_m
    map: [ "enum.HumanoidVisualLayers.Head" ]
  - state: closed
    map: [ "custom-string-key" ]
```

Use enum keys when adding shared/system-owned layers. Use strings for inventory slot bookmarks and simple local visual layers.

Important runtime API:

```csharp
_sprite.LayerMapTryGet(uid, HumanoidVisualLayers.Head, out var index, true);
_sprite.LayerSetData(uid, index, layerData);
_sprite.LayerSetColor(uid, "my-layer", Color.Red);
_sprite.LayerSetVisible(uid, HumanoidVisualLayers.Hair, false);
```

## Simple Entity Sprites

Most items, structures, and non-custom mobs have a direct `Sprite` component. They may use one layer or many explicit layers.

Simple item:

```yaml
- type: Sprite
  sprite: _Mythos/_OV/Roguetown/Weapons/swords32.rsi
  state: longsword
```

Simple layered entity:

```yaml
- type: Sprite
  sprite: Structures/Doors/Airlocks/Standard/basic.rsi
  layers:
  - state: closed
    map: [ "enum.DoorVisualLayers.Base" ]
  - state: bolted_unlit
    shader: unshaded
    map: [ "enum.DoorVisualLayers.BaseBolted" ]
```

Non-humanoid mobs generally work the same way: their prototype owns a `Sprite` with a state or explicit layers. They do not use organ-driven humanoid customization unless they inherit that body/appearance stack.

## Humanoid Sprite Stack

Humanoid player sprites start from `BaseSpeciesLayers` in `Resources/Prototypes/Body/species_appearance.yml`. It creates empty mapped sprite layers in the intended draw order. Organs, markings, and clothing fill those layers later.

Current base order:

| Order | Layer or slot | Purpose |
| --- | --- | --- |
| 1 | `Chest` | torso body part |
| 2 | `Head`, `Snout`, `Eyes` | head base and face parts |
| 3 | `RArm`, `LArm`, `RLeg`, `LLeg` | limbs |
| 4 | `UndergarmentBottom`, `UndergarmentTop`, `jumpsuit` | underwear and inner clothing |
| 5 | `LFoot`, `RFoot`, `LHand`, `RHand` | extremities |
| 6 | `Overlay` | body overlays below equipment |
| 7 | `gloves`, `shoes`, `ears`, `eyes`, `belt`, `id`, `outerClothing`, `back`, `neck`, `suitstorage` | equipment slot bookmarks |
| 8 | `SnoutCover`, `FacialHair`, `Hair`, `HeadSide`, `HeadTop`, `Tail`, `TailOverlay` | anatomical/facial/customization layers in front of equipment |
| 9 | `mask`, `head`, `pocket1`, `pocket2` | front equipment slot bookmarks |
| 10 | `Handcuffs` | overlay layer, hidden by default |

This order is why hair and tails can appear in front of many worn items, and why helmets need `HideLayerClothing` to hide hair, snouts, ears, or frills.

Important caveat: `HumanoidVisualLayers.Special` exists and `CatEars` uses it, but the current `BaseSpeciesLayers` file does not reserve a `Special` layer map. A marking whose `bodyPart` has no mapped layer will not be inserted by `VisualBodySystem`. Prefer `HeadTop` for visible ears unless you also add a mapped `Special` layer in the species appearance stack.

Human currently has `Tail` and `Special` limits set to `0` in `Resources/Prototypes/Body/Species/human.yml`. Enabling tails, cat parts, or similar fantasy options for humans or human-derived ancestries requires both a valid layer map and a markings-group limit above zero.

## Body Construction

Species prototypes select both the real mob and the character editor doll:

```yaml
- type: species
  id: Human
  prototype: MobHuman
  dollPrototype: AppearanceHuman
  skinColoration: HumanToned
```

`AppearanceHuman` inherits `BaseSpeciesAppearance`, adds `InitialBody`, and lists organ entity prototypes:

```yaml
- type: InitialBody
  organs:
    Torso: OrganHumanTorso
    Head: OrganHumanHead
    ArmLeft: OrganHumanArmLeft
    HandLeft: OrganHumanHandLeft
    Eyes: OrganHumanEyes
```

The flow:

1. The player profile stores `Species`, `Sex`, `Gender`, age, loadouts, and `HumanoidCharacterAppearance`.
2. `InitialBodySystem` spawns the organ prototypes into the body container.
3. Each visible organ has `VisualOrgan`. It targets one `HumanoidVisualLayers` layer and supplies `PrototypeLayerData`.
4. `SharedVisualBodySystem` applies profile data: skin color to body organs, eye color to eyes, and sex-specific states where configured.
5. `Content.Client/Body/VisualBodySystem.cs` writes each organ into the body sprite using `LayerMapTryGet` and `LayerSetData`.

Organ visual example:

```yaml
- type: entity
  parent: [ OrganBaseTorsoSexed, OrganBaseTorso, OrganHumanExternal ]
  id: OrganHumanTorso

- type: entity
  id: OrganHumanExternal
  abstract: true
  components:
  - type: Sprite
    sprite: Mobs/Species/Human/parts.rsi
  - type: VisualOrgan
    data:
      sprite: Mobs/Species/Human/parts.rsi
  - type: VisualOrganMarkings
    markingData:
      group: Human
```

Sexed state example:

```yaml
- type: VisualOrgan
  data:
    state: torso_m
  sexStateOverrides:
    Male: torso_m
    Female: torso_f
```

## Humanoid Visual Layers

Defined in `Content.Shared/Humanoid/HumanoidVisualLayers.cs`:

```csharp
public enum HumanoidVisualLayers : byte
{
    Special,
    Tail,
    TailOverlay,
    Hair,
    FacialHair,
    UndergarmentTop,
    UndergarmentBottom,
    Chest,
    Head,
    Snout,
    SnoutCover,
    HeadSide,
    HeadTop,
    Eyes,
    RArm,
    LArm,
    RHand,
    LHand,
    RLeg,
    LLeg,
    RFoot,
    LFoot,
    Overlay,
    Handcuffs,
    StencilMask,
    Ensnare,
    Fire
}
```

Layer meaning for customization:

- `Chest`: torso. Also owns many torso-related markings.
- `Head`: head base.
- `Eyes`: eye sprite, colored by profile eye color.
- `Hair`, `FacialHair`: hair markings.
- `Snout`, `SnoutCover`: protruding face part and overlays such as noses or snout details.
- `HeadSide`: side features, such as frills.
- `HeadTop`: top features, such as ears, horns, antennae.
- `Tail`: tail base marking.
- `TailOverlay`: markings that draw above a tail, such as rings.
- `UndergarmentTop`, `UndergarmentBottom`: profile/marking driven underwear.
- Limb layers: arm, hand, leg, foot markings.
- `Overlay`, `Handcuffs`, `Ensnare`, `Fire`, `StencilMask`: special overlays and visual effects.

Dependent layers are defined in `HumanoidVisualLayersExtension.Sublayers`. Hiding `Head` can imply head, eyes, head-side, head-top, hair, facial hair, snout, and snout-cover. Hiding `Snout` can also hide `SnoutCover`. Hiding `Chest` can hide `Tail`.

## Markings and Anatomical Customization

Hair, facial hair, tails, ears, horns, snouts, frills, tattoos, overlays, and underwear are all `marking` prototypes. A marking is not a body organ. It is one or more RSI sprite layers inserted above a mapped body layer.

Marking prototype shape:

```yaml
- type: marking
  id: HumanLongEars
  bodyPart: HeadTop
  forcedColoring: true
  groupWhitelist: [Human]
  sprites:
  - sprite: Mobs/Customization/ears.rsi
    state: long_ears_standard
```

Multi-layer marking:

```yaml
- type: marking
  id: VulpEar
  bodyPart: HeadTop
  groupWhitelist: [ Vulpkanin ]
  sprites:
  - sprite: Mobs/Customization/Vulpkanin/ear_markings.rsi
    state: vulp
  - sprite: Mobs/Customization/Vulpkanin/ear_markings.rsi
    state: vulp-inner
```

Tail example:

```yaml
- type: marking
  id: CatTail
  bodyPart: Tail
  groupWhitelist: [Human]
  sprites:
  - sprite: Mobs/Customization/cat_parts.rsi
    state: tail_cat
```

How markings render:

1. `HumanoidCharacterAppearance.Markings` stores markings by organ category, then by `HumanoidVisualLayers`.
2. `MarkingManager.EnsureValid` removes invalid species, group, layer, sex, color-count, and limit combinations.
3. `VisualOrganMarkingsComponent` declares which layers an organ may provide. The head organ provides `Head`, `Hair`, `FacialHair`, `Snout`, `SnoutCover`, `HeadSide`, and `HeadTop`. The torso organ provides `Chest`, `Tail`, `Overlay`, `TailOverlay`, underwear, and `Special`.
4. `SharedVisualBodySystem` resolves forced colors. Markings can match skin, eye color, or earlier category colors depending on their coloring rules.
5. The client `VisualBodySystem` finds the base mapped layer for `proto.BodyPart`.
6. For each marking sprite, it creates or updates a layer ID like `<MarkingId>-<RsiState>` and inserts it at `baseIndex + i + 1`.
7. Clothing layer hiding events also hide applicable marking layers.

Rules for new markings:

- `bodyPart` must be one of the humanoid visual layers and must exist in the target species sprite stack.
- `sprites` count must match the stored marking color count. `EnsureValidColors` resets mismatches.
- Use `groupWhitelist` for ancestry-specific options.
- Put ears, horns, and antennae on `HeadTop`.
- Put frills and side fins on `HeadSide`.
- Put muzzle geometry on `Snout`; put noses, markings, and overlays on `SnoutCover`.
- Put tail body shape on `Tail`; put tail rings or patterns on `TailOverlay`.
- Set `canBeDisplaced: false` if a marking does not fit standard displacement maps.
- If a layer must always exist, configure it in the relevant `markingsGroup` `limits` as `required: true` with a `default`.

Markings group example:

```yaml
- type: markingsGroup
  parent: Undergarments
  id: Human
  limits:
    enum.HumanoidVisualLayers.HeadTop:
      limit: 1
      required: false
    enum.HumanoidVisualLayers.Tail:
      limit: 0
      required: false
```

## Clothing Attachment

Clothing attaches visually by inserting layers into the wearer sprite. It does not attach to organ entities.

Inventory slots come from templates:

```yaml
- type: inventoryTemplate
  id: human
  slots:
  - name: head
    slotFlags: HEAD
  - name: jumpsuit
    slotFlags: INNERCLOTHING
  - name: outerClothing
    slotFlags: OUTERCLOTHING
```

The wearer sprite stack has matching string layer bookmarks such as `head`, `jumpsuit`, `outerClothing`, `shoes`, and `gloves`. When an item is equipped:

1. Shared `ClothingSystem` records `ClothingComponent.InSlot` and `InSlotFlag`.
2. Client `ClientClothingSystem.RenderEquipment` removes old visual layers for that slot.
3. It raises `GetEquipmentVisualsEvent` on the clothing item.
4. The clothing item supplies explicit `clothingVisuals` for the slot, species-specific `clothingVisuals`, or default visuals derived from the item RSI.
5. The client finds the slot bookmark layer on the wearer sprite.
6. It inserts equipment layers immediately after that bookmark, applies color/scale/data, and applies slot offset or displacement maps if present.
7. It stores the inserted layer keys in `InventorySlotsComponent.VisualLayerKeys` so unequip can remove them.

Explicit clothing visuals:

```yaml
- type: Clothing
  slots: [ OUTERCLOTHING ]
  clothingVisuals:
    outerClothing:
    - sprite: _Mythos/_OV/Roguetown/Clothing/onmob/armor.rsi
      state: cuirass
      map: [ "mythos-cuirass" ]
```

Species-specific visuals use `<slot>-<SpeciesId>`:

```yaml
- type: Clothing
  clothingVisuals:
    outerClothing-Dwarf:
    - sprite: _Mythos/_OV/Roguetown/Clothing/onmob/armor.rsi
      state: cuirass_dwarf
    outerClothing:
    - sprite: _Mythos/_OV/Roguetown/Clothing/onmob/armor.rsi
      state: cuirass
```

Default clothing visuals are generated when `clothingVisuals` is missing:

- It uses `ClothingComponent.rsiPath` if set.
- Otherwise it uses the item entity's base `Sprite` RSI.
- It maps modern slot names to legacy state names, for example `head -> HELMET`, `jumpsuit -> INNERCLOTHING`, `outerClothing -> OUTERCLOTHING`, `shoes -> FEET`.
- It looks for `equipped-<SLOT>`, or `<equippedPrefix>-equipped-<SLOT>`, or `equippedState` exactly.
- If a species-specific state exists, it prefers `<state>-<SpeciesId>`.

Default visual example:

```yaml
- type: Sprite
  sprite: Clothing/Head/Hats/wizard.rsi
  state: icon
- type: Clothing
  slots: [ HEAD ]
```

The item above needs `Clothing/Head/Hats/wizard.rsi/equipped-HELMET.png` or an `equipped-HELMET` state in `meta.json` to render on the wearer by default.

Use explicit `clothingVisuals` for Mythos clothing. Imported Roguetown assets often use names like `cuirass`, `cuirass_f`, `cuirass_dwarf`, and detailed helper states. Those do not match SS14 default `equipped-<SLOT>` names.

## Hiding Hair, Ears, Snouts, and Tails

Clothing hides anatomical layers through `HideLayerClothing`. It updates the wearer `HideableHumanoidLayersComponent`, and the client toggles mapped layers plus dependent marking layers.

Modern form:

```yaml
- type: HideLayerClothing
  layers:
    enum.HumanoidVisualLayers.Hair: HEAD
    enum.HumanoidVisualLayers.FacialHair: HEAD
    enum.HumanoidVisualLayers.Snout: HEAD
    enum.HumanoidVisualLayers.HeadTop: HEAD
    enum.HumanoidVisualLayers.HeadSide: HEAD
```

Older prototypes may still use:

```yaml
- type: HideLayerClothing
  slots:
  - Hair
  - Snout
  - HeadTop
  - HeadSide
  - FacialHair
```

Important behavior:

- The hide request is tied to the clothing item slot flags. The same item can hide different layers in different slots if configured.
- Multiple worn items can hide the same layer. The layer becomes visible only after all source slot flags stop hiding it.
- `HideOnToggle` means the hide behavior depends on mask toggle state.
- Markings follow hide events through `VisualOrganMarkingsComponent.HideableLayers` and `DependentHidingLayers`.

Use this for helmets, hoods, masks, cloaks, or fantasy headgear that should cover ears, horns, hair, frills, or snouts. Do not remove the marking from the profile to hide it.

## Displacement Maps

Displacement maps deform clothing or markings to fit sex/species body shapes.

Examples:

- `Resources/Prototypes/Body/Species/human.yml` defines female `jumpsuit` displacement for human appearance.
- `InventoryComponent.Displacements`, `MaleDisplacements`, and `FemaleDisplacements` are consulted by `ClientClothingSystem.RenderEquipment`.
- `VisualOrganMarkingsComponent.MarkingsDisplacement` can apply displacement to markings if the marking allows it.

Agent rule: if a fantasy asset already has species-specific states, prefer explicit states first. Use displacement only when the shape is meant to be deformed from a shared base.

## Player Customization Flow

Profile data:

```csharp
public sealed partial class HumanoidCharacterProfile
{
    public ProtoId<SpeciesPrototype> Species { get; set; }
    public Sex Sex { get; private set; }
    public Gender Gender { get; private set; }
    public HumanoidCharacterAppearance Appearance { get; set; }
}
```

Appearance data:

```csharp
public sealed partial class HumanoidCharacterAppearance
{
    public Color EyeColor { get; set; }
    public Color SkinColor { get; set; }
    public Dictionary<ProtoId<OrganCategoryPrototype>,
        Dictionary<HumanoidVisualLayers, List<Marking>>> Markings { get; set; }
}
```

Validation:

- Profile validation checks species, allowed sex, age, name, loadouts, and then appearance.
- Appearance validation clamps colors through the species skin-color strategy.
- Marking validation derives expected organs from the species `dollPrototype` `InitialBody`.
- Invalid markings are removed if the species does not have the organ, organ marking data, group permission, layer, or limit.

For new Mythos ancestry customization:

1. Add species prototype in `Resources/Prototypes/Species/`.
2. Add appearance/mob/body prototypes in `Resources/Prototypes/Body/Species/`.
3. Ensure the appearance inherits `BaseSpeciesAppearance` or supplies equivalent sprite layer bookmarks.
4. Add organ prototypes or reuse human organs with new sprite paths.
5. Add or extend a `markingsGroup` with layer limits.
6. Add marking prototypes under `Resources/Prototypes/Entities/Mobs/Customization/Markings/` or a Mythos-specific equivalent.
7. Add RSI files under `Resources/Textures/Mobs/Customization/` or `Resources/Textures/_Mythos/`.
8. Confirm head/torso organs expose the layers your markings use through `VisualOrganMarkings.markingData.layers`.
9. Add `HideLayerClothing` to helmets, hoods, and masks that should cover those layers.
10. Check the doll prototype in character setup, not only the live mob.

## Mythos Asset Notes

Imported Roguetown clothing assets currently live under:

```text
Resources/Textures/_Mythos/_OV/Roguetown/Clothing/
Resources/Textures/_Mythos/_OV/Roguetown/Clothing/onmob/
```

The top-level folders usually contain item icons. The `onmob` folders contain wearer states. Some imported RSIs have many non-SS14 state names such as `cuirass`, `cuirass_f`, `cuirass_dwarf`, `r_openrobe`, `l_openrobe`, and helper/detail states.

Recommended Mythos clothing prototype pattern:

```yaml
- type: entity
  id: ClothingOuterMythosCuirass
  parent: ClothingOuterBase
  name: cuirass
  components:
  - type: Sprite
    sprite: _Mythos/_OV/Roguetown/Clothing/armor.rsi
    state: cuirass
  - type: Clothing
    slots: [ OUTERCLOTHING ]
    clothingVisuals:
      outerClothing:
      - sprite: _Mythos/_OV/Roguetown/Clothing/onmob/armor.rsi
        state: cuirass
      outerClothing-Dwarf:
      - sprite: _Mythos/_OV/Roguetown/Clothing/onmob/armor.rsi
        state: cuirass_dwarf
```

If an imported asset is split into left/right sleeve or detail states, add multiple layers under the same slot. Give each layer a unique `map` key if another visualizer needs to alter it later.

```yaml
clothingVisuals:
  outerClothing:
  - sprite: _Mythos/_OV/Roguetown/Clothing/onmob/armor.rsi
    state: openrobe
    map: [ "mythos-openrobe-body" ]
  - sprite: _Mythos/_OV/Roguetown/Clothing/onmob/armor.rsi
    state: r_openrobe
    map: [ "mythos-openrobe-right" ]
  - sprite: _Mythos/_OV/Roguetown/Clothing/onmob/armor.rsi
    state: l_openrobe
    map: [ "mythos-openrobe-left" ]
```

## Common Failure Modes

- Missing wearer sprite: entity has no `SpriteComponent`; nothing can render.
- Missing RSI state: `state` in YAML does not exist in `meta.json`.
- Wrong RSI frame size: PNG dimensions are not a multiple of `meta.json` `size`.
- Wrong direction count: a 4-direction body state needs all four directions in the expected order.
- Marking body layer missing: `bodyPart` has no layer map in `BaseSpeciesLayers` or the species equivalent.
- Marking never appears: organ `VisualOrganMarkings.markingData.layers` does not include that layer, or group limits disallow it.
- Marking color mismatch: number of stored colors differs from number of marking sprites.
- Clothing item visible in inventory only: item has icon state but no on-mob `equipped-*` state and no explicit `clothingVisuals`.
- Clothing draws behind or in front of the wrong layer: slot bookmark is wrong, or the species appearance layer order needs adjustment.
- Helmet does not hide ears/hair: missing `HideLayerClothing`, wrong layer names, or obsolete `slots` form not matching desired slot flags.
- Mythos imported clothing does not show: default SS14 state names are missing. Add explicit `clothingVisuals`.

## Agent Search Terms

```powershell
rg -n "class ClientClothingSystem|RenderEquipment|GetEquipmentVisualsEvent|TryGetDefaultVisuals" Content.Client Content.Shared
rg -n "HideLayerClothing|HideableHumanoidLayers|HumanoidLayerVisibilityChangedEvent" Content.Client Content.Shared Resources\\Prototypes
rg -n "VisualOrgan|VisualOrganMarkings|ApplyMarkings|LayerMapTryGet\\(target, proto.BodyPart" Content.Client Content.Shared Resources\\Prototypes
rg -n "enum.HumanoidVisualLayers|bodyPart: Tail|bodyPart: HeadTop|bodyPart: HeadSide|bodyPart: Snout|bodyPart: SnoutCover" Content.Shared Resources\\Prototypes
rg -n "type: markingsGroup|limits:|groupWhitelist|forcedColoring|canBeDisplaced" Resources\\Prototypes\\Body Resources\\Prototypes\\Entities\\Mobs\\Customization\\Markings -g "*.yml"
rg -n "clothingVisuals:|equippedPrefix|equippedState|slots: \\[ HEAD|slots: \\[ OUTERCLOTHING|slots: \\[ INNERCLOTHING" Resources\\Prototypes\\Entities\\Clothing -g "*.yml"
rg --files Resources\\Textures\\Mobs\\Species Resources\\Textures\\Mobs\\Customization Resources\\Textures\\_Mythos | rg "\\.rsi(\\\\|/)(meta\\.json|.*\\.png)$"
```
