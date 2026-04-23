# Agent Search Index

This page is a fast command sheet for future agents. Run commands from the repository root: `c:\Users\magne\Documents\Mythos\Mythos`.

## Global Orientation

```powershell
rg --files Content.Client Content.Server Content.Shared Resources RobustToolbox
rg --files -g "*.csproj" -g "*.slnx" -g "*.props" -g "*.targets"
rg -n "Program|EntryPoint|ContentStart|PreInit|PostInit" Content.Client Content.Server Content.Shared RobustToolbox
```

## ECS

```powershell
rg -n "class .*Component|sealed partial class .*Component|\\[RegisterComponent\\]" Content.Client Content.Server Content.Shared
rg -n "class .*System|sealed class .*System|EntitySystem" Content.Client Content.Server Content.Shared
rg -n "SubscribeLocalEvent|SubscribeNetworkEvent|RaiseLocalEvent|RaiseNetworkEvent|Dirty\\(" Content.Client Content.Server Content.Shared
```

## Prototypes and Data

```powershell
rg -n "\\[Prototype|IPrototype|DataField|IdDataField" Content.Client Content.Server Content.Shared
rg -n "type: entity|parent:|components:" Resources\\Prototypes\\Entities -g "*.yml"
rg -n "type: job|type: species|type: gameMap|type: uiTheme|type: action" Resources\\Prototypes -g "*.yml"
```

## UI

```powershell
rg --files Content.Client\\UserInterface -g "*.xaml" -g "*.xaml.cs" -g "*UIController.cs"
rg -n "BoundUserInterface|BoundUserInterfaceState|BoundUserInterfaceMessage|SetUiState" Content.Client Content.Server Content.Shared
rg -n "uiTheme|StyleClasses|Stylesheet|Sheetlet|Palette|InterfaceTheme" Content.Client Resources\\Prototypes
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
```

## Player Entities and Roles

```powershell
rg -n "type: species|prototype:|dollPrototype:" Resources\\Prototypes\\Species -g "*.yml"
rg -n "MobHuman|BaseSpeciesMob|InitialBody|HumanoidProfile|Inventory" Resources\\Prototypes\\Body Resources\\Prototypes\\Entities\\Mobs -g "*.yml"
rg -n "type: job|startingGear|access:|supervisors|playTimeTracker" Resources\\Prototypes\\Roles\\Jobs -g "*.yml"
```

## Interactions, Combat, and AI

```powershell
rg -n "GetClickedEntity|Clickable|ActivateItemInWorld|UseItemInHand|Verb|GetVerbs" Content.Client Content.Shared Content.Server
rg -n "Action|TargetAction|InstantAction|WorldTargetAction|Hotbar" Content.Client Content.Shared Resources\\Prototypes
rg -n "Damageable|MobThresholds|MobState|Stamina|StatusEffects|MeleeWeapon|GunComponent" Content.Shared Content.Server Resources\\Prototypes
rg -n "HTN|NPC|NpcFaction|UtilityQuery|NPCMeleeCombat|NPCRangedCombat|NPCSteering" Content.Server Content.Shared Resources\\Prototypes
```

