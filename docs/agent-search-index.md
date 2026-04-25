# Agent Search Index

This page is a fast command sheet for future agents. Run commands from the repository root: `c:\Users\magne\Documents\Mythos Dev\Mythos`.

## Global Orientation

```powershell
rg --files Content.Client Content.Server Content.Shared Resources RobustToolbox
rg --files -g "*.csproj" -g "*.slnx" -g "*.props" -g "*.targets"
rg -n "Program|EntryPoint|ContentStart|PreInit|PostInit" Content.Client Content.Server Content.Shared RobustToolbox
rg -n "Mythos|_Mythos|// Mythos:" Content.Client Content.Server Content.Shared Resources Tools
```

## Mythos Fork

```powershell
rg --files Content.Client\_Mythos Content.Server\_Mythos Content.Shared\_Mythos Resources\Prototypes\_Mythos Tools\_Mythos
rg -n "namespace Content\.(Client|Server|Shared)\.Mythos|namespace Content\.(Client|Server|Shared)\._Mythos" Content.Client\_Mythos Content.Server\_Mythos Content.Shared\_Mythos
rg -n "MythosSpellLoadout|ManaComponent|CombatTargetComponent|CombatQueueComponent|QueuedActionKind|ActionMythos" Content.Client\_Mythos Content.Server\_Mythos Content.Shared\_Mythos Resources\Prototypes\_Mythos
rg -n "// Mythos:" Content.Client Content.Server Content.Shared
```

## ECS

```powershell
rg -n "class .*Component|sealed partial class .*Component|\\[RegisterComponent\\]" Content.Client Content.Server Content.Shared
rg -n "class .*System|sealed class .*System|EntitySystem" Content.Client Content.Server Content.Shared
rg -n "SubscribeLocalEvent|SubscribeNetworkEvent|RaiseLocalEvent|RaiseNetworkEvent|Dirty\\(" Content.Client Content.Server Content.Shared
```

## Networking and Prediction

```powershell
rg -n "NetworkedComponent|AutoGenerateComponentState|AutoNetworkedField|ComponentGetState|ComponentHandleState|Dirty\\(" Content.Client Content.Server Content.Shared
rg -n "Serializable, NetSerializable|RaisePredictiveEvent|SubscribeAllEvent|EntitySessionEventArgs|NetEntity|GetNetEntity|TryGetEntity" Content.Client Content.Server Content.Shared
rg -n "IsFirstTimePredicted|ApplyingState|PredictedSpawn|PredictedQueueDelete|SendPredictedMessage" Content.Client Content.Server Content.Shared RobustToolbox
```

## Prototypes and Data

```powershell
rg -n "\\[Prototype|IPrototype|DataField|IdDataField" Content.Client Content.Server Content.Shared
rg -n "type: entity|parent:|components:" Resources\\Prototypes\\Entities -g "*.yml"
rg -n "type: job|type: species|type: gameMap|type: uiTheme|type: action" Resources\\Prototypes -g "*.yml"
rg -n "ActionMythos|Queue.*ActionEvent|SpellComponent|ManaCost|CastTime" Resources\\Prototypes\\_Mythos Content.Shared\\_Mythos Content.Server\\_Mythos -g "*.yml" -g "*.cs"
```

## UI

```powershell
rg --files Content.Client\\UserInterface -g "*.xaml" -g "*.xaml.cs" -g "*UIController.cs"
rg -n "BoundUserInterface|BoundUserInterfaceState|BoundUserInterfaceMessage|SetUiState" Content.Client Content.Server Content.Shared
rg -n "uiTheme|StyleClasses|Stylesheet|Sheetlet|Palette|InterfaceTheme" Content.Client Resources\\Prototypes
rg -n "Overlay|OverlaySystem|ManaHud|QueueHud|TargetReticle|MythosTileSpawn|MythosThemeSheetlet" Content.Client Content.Client\\_Mythos
```

## Input

```powershell
rg -n "function:|key:|mod1:" Resources\\keybinds.yml
rg -n "BoundKeyFunction|ContentKeyFunctions|EngineKeyFunctions|SetupContexts|CommandBinds" Content.Client Content.Shared RobustToolbox\\Robust.Shared
```

## Maps and Rounds

```powershell
rg -n "type: gameMap|type: gameMapPool|mapPath:|stations:|availableJobs:" Resources\\Prototypes\\Maps -g "*.yml"
rg -n "GameTicker|GamePreset|GameRule|RoundFlow|Spawning|StationJobs|StationSpawning" Content.Server Content.Shared Resources\\Prototypes
rg --files Resources\\Maps
rg -n "mapping|savemap|forcemap|BecomesStation|StationConfig" Docs Content.Server Content.Shared Resources\\Prototypes Resources\\Maps
```

## Player Entities and Roles

```powershell
rg -n "type: species|prototype:|dollPrototype:" Resources\\Prototypes\\Species -g "*.yml"
rg -n "MobHuman|BaseSpeciesMob|InitialBody|HumanoidProfile|Inventory" Resources\\Prototypes\\Body Resources\\Prototypes\\Entities\\Mobs -g "*.yml"
rg -n "type: job|startingGear|access:|supervisors|playTimeTracker" Resources\\Prototypes\\Roles\\Jobs -g "*.yml"
rg -n "PlayerAttachedEvent|MythosSpellLoadout|AddAction|ActionMythos" Content.Server\\_Mythos Content.IntegrationTests\\_Mythos Resources\\Prototypes\\_Mythos
```

## Avatar Sprites and Clothing

```powershell
rg -n "SpriteComponent|PrototypeLayerData|SpriteSpecifier|RSIResource|LayerMapTryGet|LayerMapSet" RobustToolbox Content.Client Content.Shared
rg -n "BaseSpeciesLayers|enum.HumanoidVisualLayers|VisualOrgan|VisualOrganMarkings|InitialBody|HumanoidProfile" Resources\\Prototypes\\Body Content.Shared Content.Client -g "*.yml" -g "*.cs"
rg -n "type: marking|bodyPart: Tail|bodyPart: HeadTop|bodyPart: HeadSide|bodyPart: Snout|bodyPart: SnoutCover|groupWhitelist|forcedColoring" Resources\\Prototypes\\Entities\\Mobs\\Customization\\Markings Resources\\Prototypes\\Body -g "*.yml"
rg -n "class ClientClothingSystem|RenderEquipment|GetEquipmentVisualsEvent|TryGetDefaultVisuals|HideLayerClothing|HideableHumanoidLayers" Content.Client Content.Shared Resources\\Prototypes -g "*.cs" -g "*.yml"
rg -n "clothingVisuals:|equippedPrefix|equippedState|slots: \\[ HEAD|slots: \\[ OUTERCLOTHING|slots: \\[ INNERCLOTHING" Resources\\Prototypes\\Entities\\Clothing Resources\\Prototypes\\_Mythos -g "*.yml"
rg --files Resources\\Textures\\Mobs\\Species Resources\\Textures\\Mobs\\Customization Resources\\Textures\\_Mythos | rg "\\.rsi(\\\\|/)(meta\\.json|.*\\.png)$"
```

## Interactions, Combat, and AI

```powershell
rg -n "GetClickedEntity|Clickable|ActivateItemInWorld|UseItemInHand|Verb|GetVerbs" Content.Client Content.Shared Content.Server
rg -n "Action|TargetAction|InstantAction|WorldTargetAction|Hotbar" Content.Client Content.Shared Resources\\Prototypes
rg -n "Damageable|MobThresholds|MobState|Stamina|StatusEffects|MeleeWeapon|GunComponent" Content.Shared Content.Server Resources\\Prototypes
rg -n "HTN|NPC|NpcFaction|UtilityQuery|NPCMeleeCombat|NPCRangedCombat|NPCSteering" Content.Server Content.Shared Resources\\Prototypes
rg -n "SelectCombatTargetEvent|CombatAutoAttackSystem|CombatQueueExecutor|CastMagicMissileEvent|StartCastRequestEvent|FireballDoAfterEvent" Content.Client\\_Mythos Content.Server\\_Mythos Content.Shared\\_Mythos
```

## Assets and Mapping

```powershell
rg --files Resources\\Textures\\_Mythos Resources\\Textures\\Tiles\\_Mythos Resources\\Prototypes\\_Mythos\\Tiles Tools\\_Mythos\\ov_port
rg -n "tileDefinition|variants:|edgeSprites:|sprite: /Textures/Tiles/_Mythos|baseTurf" Resources\\Prototypes\\_Mythos\\Tiles -g "*.yml"
rg -n "tiles-ov-|_Mythos" Resources\\Locale\\en-US\\_Mythos -g "*.ftl"
rg -n "MsgSetTileVariantOverride|TileVariantOverrideSystem|PlacementTileEvent|MythosTileSpawningUIController|MythosTileItemList" Content.Client\\_Mythos Content.Server\\_Mythos Content.Shared\\_Mythos Content.Client\\UserInterface\\Systems\\Sandbox
```

## Testing

```powershell
rg --files Content.Tests\\_Mythos Content.IntegrationTests\\_Mythos
rg -n "Category\\(\"Mythos\"\\)|TestOf|GameTest|InteractionTest|PoolManager|TestPair" Content.Tests\\_Mythos Content.IntegrationTests\\_Mythos -g "*.cs"
dotnet test Content.Tests/Content.Tests.csproj --filter Category=Mythos --no-restore
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter Category=Mythos --no-restore
```

