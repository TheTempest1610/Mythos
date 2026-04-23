# HUD, UI, and Themes

## Purpose

This page maps player-facing UI: the main HUD, game screens, UI controllers, XAML widgets, bound entity UIs, EUI windows, stylesheets, and theme resources. Use it for reskinning, HUD layout changes, fantasy UI chrome, or replacing machine interfaces.

## Source Anchors

- `Content.Client/Gameplay/GameplayState.cs`
- `Content.Client/UserInterface/Screens/DefaultGameScreen.xaml`
- `Content.Client/UserInterface/Screens/DefaultGameScreen.xaml.cs`
- `Content.Client/UserInterface/Screens/SeparatedChatGameScreen.xaml`
- `Content.Client/UserInterface/Systems/Gameplay/GameplayStateLoadController.cs`
- `Content.Client/UserInterface/Systems/MenuBar/Widgets/GameTopMenuBar.xaml`
- `Content.Client/UserInterface/Systems/Actions/`
- `Content.Client/UserInterface/Systems/Alerts/`
- `Content.Client/UserInterface/Systems/Chat/`
- `Content.Client/UserInterface/Systems/Hotbar/`
- `Content.Client/UserInterface/Systems/Inventory/`
- `Content.Client/Stylesheets/`
- `Resources/Prototypes/themes.yml`
- `Resources/Prototypes/hud.yml`
- `Resources/Textures/Interface/`
- `RobustToolbox/Robust.Client/UserInterface/`

## Runtime Flow

When the client enters gameplay, `GameplayState` reads `CCVars.UILayout` and loads either `DefaultGameScreen` or `SeparatedChatGameScreen`. These XAML files compose the major HUD widgets: viewport, top menu bar, actions, ghost UI, inventory, hotbar, chat, and alerts.

UI controllers connect gameplay systems to widgets. Examples:

- `ActionUIController` listens to action changes, registers hotbar keybinds, and updates action buttons.
- `InventoryUIController` creates the stripping window, updates inventory slot buttons, and handles the inventory bar.
- `ChatUIController` wires chat focus keybinds and chat UI behavior.
- `AlertsUIController` displays player status alerts.
- `HotbarUIController` owns hotbar widget behavior.

Entity-specific interfaces use Robust bound UIs. A server entity has a `UserInterface` component that maps a UI key to a bound interface class. The server sends `BoundUserInterfaceState` and receives `BoundUserInterfaceMessage`. The client has a `BoundUserInterface` class and usually a XAML window/control. This pattern drives machines, consoles, storage, scanners, and many interactable objects.

EUI is a separate extended UI path used for more global or session-specific windows, such as admin and ghost role flows.

Styles are code-driven in `Content.Client/Stylesheets/`. Theme prototypes in `Resources/Prototypes/themes.yml` define theme IDs, texture roots, and color variables. Interface art is in `Resources/Textures/Interface/`.

## Customization Levers

- Main HUD layout: edit `DefaultGameScreen.xaml`, `SeparatedChatGameScreen.xaml`, and their code-behind anchor/margin logic.
- HUD widget behavior: edit controllers under `Content.Client/UserInterface/Systems/`.
- UI colors and texture theme roots: edit `Resources/Prototypes/themes.yml`.
- UI chrome and icons: replace files under `Resources/Textures/Interface/<ThemeName>/`.
- Shared control styling: edit `Content.Client/Stylesheets/Sheetlets/`, `Palette/`, and `Stylesheets/`.
- Machine and item UIs: find the server system that calls `SetUiState`, the shared state/message classes, the client `BoundUserInterface`, and the XAML window.
- Top-level screen choice: `CCVars.UILayout` and `Content.Client/Gameplay/GameplayState.cs`.

## Fantasy Conversion Notes

Reskin first. A fantasy UI can be achieved by replacing interface textures, creating a Mythos UI theme prototype, adjusting palettes, and swapping HUD icon art. That keeps every existing machine and menu functional while the underlying sci-fi systems are gradually replaced.

Rewrite UI only when the workflow changes. For example, a cargo console becoming a guild market can probably keep the bound UI structure but needs new labels, products, and business logic. A PDA becoming a spellbook may need new screens and action integration.

Watch for text in localization, not XAML. Many UI labels are `Loc.GetString` calls or FTL keys referenced by code/prototypes.

## Agent Search Terms

```powershell
rg -n "LoadScreen|DefaultGameScreen|SeparatedChatGameScreen|CCVars.UILayout" Content.Client
rg --files Content.Client\\UserInterface -g "*.xaml" -g "*.xaml.cs" -g "*UIController.cs"
rg -n "BoundUserInterface|BoundUserInterfaceState|BoundUserInterfaceMessage|SetUiState|UserInterfaceComponent" Content.Client Content.Server Content.Shared
rg -n "uiTheme|SS14DefaultTheme|SetDefaultTheme|InterfaceTheme" Content.Client Resources\\Prototypes
rg -n "StyleClasses|Stylesheet|Sheetlet|Palette|StyleBox" Content.Client\\Stylesheets Content.Client\\UserInterface
```

