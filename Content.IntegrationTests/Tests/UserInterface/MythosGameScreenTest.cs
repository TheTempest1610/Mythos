#nullable enable
using System.Linq;
using Content.Client._Mythos.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Client.UserInterface.Systems.Inventory.Widgets;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.IntegrationTests.Fixtures;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests.UserInterface;

/// <summary>
/// Phase 1 acceptance tests for the Mythos HUD reskin.
/// Batched into a single fixture so the pooled client/server pair is reused across cases.
/// </summary>
[TestFixture]
public sealed class MythosGameScreenTest : GameTest
{
    [Test]
    public async Task Phase1_ScreenStructure()
    {
        await Client.WaitAssertion(() =>
        {
            var activator = IoCManager.Resolve<IDynamicTypeFactory>();
            var screen = activator.CreateInstance<MythosGameScreen>();

            Assert.Multiple(() =>
            {
                // 1. Top-level roots exist.
                Assert.That(FindByName<LayoutContainer>(screen, "ViewportContainer"), Is.Not.Null,
                    "ViewportContainer missing");
                Assert.That(FindByName<LayoutContainer>(screen, "OldHudRoot"), Is.Not.Null,
                    "OldHudRoot missing");
                Assert.That(FindByName<LayoutContainer>(screen, "MythosHudRoot"), Is.Not.Null,
                    "MythosHudRoot missing");

                // 2. Upstream widgets mounted in OldHudRoot so upstream UIControllers' lookups resolve.
                //    HotbarGui is intentionally NOT mounted - the Mythos screen ships its own MythosHotbar.
                Assert.That(FindByName<GameTopMenuBar>(screen, "TopBar"), Is.Not.Null, "TopBar missing");
                Assert.That(FindByName<ActionsBar>(screen, "Actions"), Is.Not.Null, "ActionsBar missing");
                Assert.That(FindByName<GhostGui>(screen, "Ghost"), Is.Not.Null, "GhostGui missing");
                Assert.That(FindByName<InventoryGui>(screen, "Inventory"), Is.Not.Null, "InventoryGui missing");
                Assert.That(FindByName<AlertsUI>(screen, "Alerts"), Is.Not.Null, "AlertsUI missing");

                // 3. ChatBox property returns a valid chat instance for ChatUIController auto-registration.
                Assert.That(screen.ChatBox, Is.Not.Null, "ChatBox property null");
                Assert.That(screen.ChatBox, Is.InstanceOf<ChatBox>());

                // 4. OldHudRoot starts hidden (default Mythos screen state).
                var oldHud = FindByName<LayoutContainer>(screen, "OldHudRoot")!;
                Assert.That(oldHud.Visible, Is.False, "OldHudRoot should default to hidden in Mythos screen");

                // 5. SetOldHudVisible toggles correctly.
                screen.SetOldHudVisible(true);
                Assert.That(oldHud.Visible, Is.True, "SetOldHudVisible(true) failed");
                screen.SetOldHudVisible(false);
                Assert.That(oldHud.Visible, Is.False, "SetOldHudVisible(false) failed");

                // 6. Default occlusion contract from base InGameScreen still applies (no Mythos widgets occlude yet).
                var (left, right) = screen.GetMythosViewportOcclusionPixels();
                Assert.That(left, Is.EqualTo(0f), "Phase 1 should report zero left occlusion");
                Assert.That(right, Is.EqualTo(0f), "Phase 1 should report zero right occlusion");
            });
        });
    }

    /// <summary>
    /// Recursively searches the control tree for a named child of the requested type.
    /// </summary>
    private static T? FindByName<T>(Control root, string name) where T : Control
    {
        if (root.Name == name && root is T match)
            return match;

        foreach (var child in root.Children)
        {
            if (FindByName<T>(child, name) is { } found)
                return found;
        }

        return null;
    }
}
