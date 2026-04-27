#nullable enable
using Content.Client._Mythos.UserInterface.Screens;
using Content.Client._Mythos.UserInterface.Stylesheets;
using Content.Client._Mythos.UserInterface.Systems.Chat.Widgets;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.IntegrationTests.Fixtures;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests.UserInterface;

/// <summary>
/// Phase 4 acceptance tests: MythosChatBox subclass registers with ChatUIController,
/// applies cyan theming via inherited XAML, and is wired in MythosGameScreen.
/// </summary>
[TestFixture]
public sealed class MythosHudPhase4Test : GameTest
{
    [Test]
    public async Task Phase4_ChatAndTabRestyle()
    {
        await Client.WaitAssertion(() =>
        {
            var activator = IoCManager.Resolve<IDynamicTypeFactory>();

            Assert.Multiple(() =>
            {
                // 1. MythosChatBox subclass still instantiates and applies the Mythos
                //    frame style class to the inherited ChatWindowPanel (kept available
                //    for callers that want a self-styled chat without MythosChatPanel chrome).
                var chat = activator.CreateInstance<MythosChatBox>();
                Assert.That(chat, Is.Not.Null);
                Assert.That(chat.ChatWindowPanel, Is.Not.Null,
                    "Inherited ChatWindowPanel must resolve");
                Assert.That(chat.ChatWindowPanel.StyleClasses,
                    Contains.Item(MythosPalette.FrameClass),
                    "MythosChatBox should add the Mythos frame style class");

                // 2. MythosGameScreen mounts the overhaul's MythosChatPanel (chrome + collapse)
                //    wrapping a plain ChatBox. Both must resolve.
                var screen = activator.CreateInstance<MythosGameScreen>();
                var chatPanel = FindByName<Content.Client.Mythos.UserInterface.Chat.MythosChatPanel>(screen, "ChatPanel");
                Assert.That(chatPanel, Is.Not.Null,
                    "MythosGameScreen should mount MythosChatPanel chrome");

                var chatInScreen = FindByName<ChatBox>(screen, "Chat");
                Assert.That(chatInScreen, Is.Not.Null, "Chat ChatBox missing inside ChatPanel");
                Assert.That(screen.ChatBox, Is.SameAs(chatInScreen));

                // 3. Stylesheet rules registered (MythosTabSheetlet adds MenuButton rules).
                var ui = IoCManager.Resolve<IUserInterfaceManager>();
                Assert.That(ui.Stylesheet?.Rules, Is.Not.Null.And.Not.Empty);
            });
        });
    }

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
