# Input and Controls

## Purpose

This page maps keyboard and mouse control flow from YAML keybinds to input contexts and command handlers. Use it when changing default controls, adding fantasy actions, modifying movement, or replacing interaction patterns.

## Source Anchors

- `Resources/keybinds.yml`
- `Content.Shared/Input/ContentKeyFunctions.cs`
- `Content.Client/Input/ContentContexts.cs`
- `RobustToolbox/Robust.Shared/Input/KeyFunctions.cs`
- `RobustToolbox/Robust.Shared/Input/Binding/`
- `Content.Client/Gameplay/GameplayStateBase.cs`
- `Content.Shared/Movement/Systems/SharedMoverController.Input.cs`
- `Content.Shared/Interaction/SharedInteractionSystem.cs`
- `Content.Shared/Hands/EntitySystems/SharedHandsSystem.Interactions.cs`
- `Content.Client/UserInterface/Systems/Actions/ActionUIController.cs`
- `Content.Client/Options/UI/Tabs/KeyRebindTab.xaml.cs`

## Runtime Flow

Default keybinds are data in `Resources/keybinds.yml`. Each bind maps a physical key or mouse button to a named function such as `MoveUp`, `ActivateItemInWorld`, `OpenInventoryMenu`, or `Hotbar1`.

Key function names come from two places:

- engine functions in `RobustToolbox/Robust.Shared/Input/KeyFunctions.cs`
- content functions in `Content.Shared/Input/ContentKeyFunctions.cs`

The client registers which functions are legal in each input context through `Content.Client/Input/ContentContexts.cs`. Important contexts include:

- `common`: chat, examine, screenshot, guidebook, admin/menu actions, actions menu, hotbar keys
- `human`: movement, hands, inventory, pulling, crafting, item activation, object rotation, arcade inputs
- `ghost`: ghost movement and inherited human/common commands
- `aghost`: admin ghost movement and interaction

Systems then bind functions to behavior through `CommandBinds` or direct input manager commands. Examples:

- movement uses `SharedMoverController.Input.cs`
- interaction uses `SharedInteractionSystem.cs`
- hands use `SharedHandsSystem.Interactions.cs`
- actions/hotbar use `ActionUIController.cs`
- chat focus uses `ChatUIController.cs`
- inventory open uses `InventoryUIController.cs`

Gameplay clicks pass through `GameplayStateBase`, which finds clickable entities under the viewport and forwards input into the simulation.

## Customization Levers

- Change default controls in `Resources/keybinds.yml`.
- Add a new action key by adding a `BoundKeyFunction` to `ContentKeyFunctions`, adding it to the relevant context in `ContentContexts`, adding a default bind, then registering a command handler.
- Add ability-style controls through the existing actions/hotbar system before inventing a separate hotkey framework.
- For new creature control modes, add a new input context or reuse `human` with different components.
- Expose new binds in the options menu by updating `KeyRebindTab.xaml.cs`.

## Fantasy Conversion Notes

Fantasy gameplay should reuse the action system for spells, prayers, songs, class skills, stances, and item powers. That automatically integrates with the hotbar, targeting flow, cooldown presentation, and keybinds.

Only change core movement if the game mode requires it. Many systems assume the engine movement functions and `InputMover`/`MobMover` chain. Mounts, possession, flying, stealth, or shapeshift forms are better introduced as movement modifiers and components before replacing input infrastructure.

## Agent Search Terms

```powershell
rg -n "function:|key:|mod1:|type:" Resources\\keybinds.yml
rg -n "BoundKeyFunction|ContentKeyFunctions|EngineKeyFunctions" Content.Shared RobustToolbox\\Robust.Shared
rg -n "SetupContexts|AddFunction|contexts.New" Content.Client\\Input
rg -n "CommandBinds.Builder|SetInputCommand|InputCmdHandler|PointerInputCmdHandler" Content.Client Content.Shared Content.Server
rg -n "ActivateItemInWorld|UseItemInHand|OpenActionsMenu|Hotbar|MoveUp|Walk" Content.Client Content.Shared Content.Server
```

