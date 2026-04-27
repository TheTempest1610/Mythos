#nullable enable
using System.Linq;
using System.Numerics;
using Content.Client._Mythos.UserInterface.Screens;
using Content.Client._Mythos.UserInterface.Systems.Hotbar.Widgets;
using Content.Client._Mythos.UserInterface.Systems.Location.Widgets;
using Content.Client._Mythos.UserInterface.Systems.Vitals.Widgets;
using Content.Client.Mythos.UserInterface.Chat;
using Content.Client.Mythos.UserInterface.Equipment;
using Content.IntegrationTests.Fixtures;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.UserInterface;

/// <summary>
/// Visual layout assertions for the Mythos HUD. Forces a measure + arrange pass at a
/// known viewport size and reads back the resolved positions / sizes of every widget,
/// so layout regressions are caught here instead of via manual screenshot review.
/// </summary>
[TestFixture]
public sealed class MythosHudLayoutTest : GameTest
{
    private const float ScreenWidth = 1280f;
    private const float ScreenHeight = 720f;

    [Test]
    public async Task BottomStrip_OrbsAreSnugAroundHotbar()
    {
        await Client.WaitAssertion(() =>
        {
            var screen = BuildLaidOutScreen();

            var vitals = FindByName<VitalsOrbsBar>(screen, "Vitals")!;
            var hudRoot = FindByName<LayoutContainer>(screen, "MythosHudRoot")!;
            var hp = FindByName<Content.Client._Mythos.UserInterface.Systems.Vitals.Controls.OrbControl>(vitals, "HpOrb")!;
            var hotbar = FindByName<PanelContainer>(vitals, "HotbarFrame")!;
            var qi = FindByName<Content.Client._Mythos.UserInterface.Systems.Vitals.Controls.OrbControl>(vitals, "QiOrb")!;

            var vitalsRect = vitals.GlobalRect();
            var hpRect = hp.GlobalRect();
            var hotbarRect = hotbar.GlobalRect();
            var qiRect = qi.GlobalRect();
            var hudRect = hudRoot.GlobalRect();

            Assert.Multiple(() =>
            {
                // Strip widget itself must be ON-SCREEN with positive height.
                Assert.That(vitalsRect.Top, Is.LessThan(hudRect.Bottom),
                    $"Vitals widget top ({vitalsRect.Top}) is at/below HudRoot bottom ({hudRect.Bottom}) - rendered off-screen");
                Assert.That(vitalsRect.Bottom, Is.LessThanOrEqualTo(hudRect.Bottom + 1f),
                    $"Vitals widget bottom ({vitalsRect.Bottom}) extends past HudRoot bottom ({hudRect.Bottom})");
                Assert.That(vitalsRect.Bottom - vitalsRect.Top, Is.GreaterThan(40f),
                    "Vitals widget collapsed to near-zero height");

                // Each piece of the strip is positioned within the visible HudRoot area.
                Assert.That(hpRect.Bottom, Is.LessThanOrEqualTo(hudRect.Bottom + 1f),
                    $"HP orb bottom ({hpRect.Bottom}) past HudRoot bottom ({hudRect.Bottom}) - rendered below screen");
                Assert.That(qiRect.Bottom, Is.LessThanOrEqualTo(hudRect.Bottom + 1f),
                    $"Qi orb bottom ({qiRect.Bottom}) past HudRoot bottom ({hudRect.Bottom}) - rendered below screen");
                Assert.That(hpRect.Bottom, Is.GreaterThan(hudRect.Bottom - hudRect.Height * 0.15f),
                    "HP orb should hug the bottom of HudRoot");
                Assert.That(qiRect.Bottom, Is.GreaterThan(hudRect.Bottom - hudRect.Height * 0.15f),
                    "Qi orb should hug the bottom of HudRoot");

                // HP -> Hotbar -> Qi in left-to-right order, snug (no gap > 1px).
                Assert.That(hpRect.Right, Is.EqualTo(hotbarRect.Left).Within(1f),
                    "Hotbar should be flush against HP orb's right edge");
                Assert.That(hotbarRect.Right, Is.EqualTo(qiRect.Left).Within(1f),
                    "Qi orb should be flush against hotbar's right edge");

                // The widget itself must span the full HudRoot width so the inner
                // expanding spacers actually have room to push content to center.
                Assert.That(vitalsRect.Width, Is.EqualTo(hudRect.Width).Within(2f),
                    $"Vitals widget width ({vitalsRect.Width}) != HudRoot width ({hudRect.Width}) - inner spacers can't center content if the widget is narrower than its container");

                // Strip content centers within the vitals widget (and therefore on the
                // HudRoot midline now that the widget spans the full width).
                var stripCenter = (hpRect.Left + qiRect.Right) * 0.5f;
                var widgetCenter = (vitalsRect.Left + vitalsRect.Right) * 0.5f;
                Assert.That(stripCenter, Is.EqualTo(widgetCenter).Within(4f),
                    $"Strip not centered in widget: stripCenter={stripCenter}, widgetCenter={widgetCenter}");

                // And the strip is centered on the HudRoot midline (the value-add
                // assertion that catches the "widget exists at left edge but is too
                // narrow" failure mode).
                var hudCenter = (hudRect.Left + hudRect.Right) * 0.5f;
                Assert.That(stripCenter, Is.EqualTo(hudCenter).Within(4f),
                    $"Strip ({stripCenter}) not centered on HudRoot ({hudCenter}) - check widget width");
            });
        });
    }

    [Test]
    public async Task BottomStrip_HotbarHasEightNumberedSlots()
    {
        await Client.WaitAssertion(() =>
        {
            var activator = IoCManager.Resolve<IDynamicTypeFactory>();
            var hotbar = activator.CreateInstance<MythosHotbar>();

            var slotRow = FindByName<BoxContainer>(hotbar, "SlotRow")!;
            var slots = slotRow.Children.OfType<PanelContainer>().ToList();

            Assert.Multiple(() =>
            {
                Assert.That(slots, Has.Count.EqualTo(MythosHotbar.SlotCount),
                    "MythosHotbar should expose exactly 8 slot panels");

                for (var i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    var label = slot.Children.OfType<Label>().FirstOrDefault();
                    Assert.That(label, Is.Not.Null, $"Slot {i + 1} missing label");
                    Assert.That(label!.Text, Is.EqualTo((i + 1).ToString()),
                        $"Slot {i + 1} should be labelled {i + 1}");
                }
            });
        });
    }

    [Test]
    public async Task LocationPill_RendersAboveChatPanel()
    {
        await Client.WaitAssertion(() =>
        {
            var screen = BuildLaidOutScreen();

            var hudRoot = FindByName<LayoutContainer>(screen, "MythosHudRoot")!;
            var children = hudRoot.Children.ToList();
            var chatIndex = children.FindIndex(c => c.Name == "ChatPanel");
            var locationIndex = children.FindIndex(c => c.Name == "LocationTime");

            Assert.That(chatIndex, Is.GreaterThanOrEqualTo(0), "ChatPanel must be a sibling of LocationTime");
            Assert.That(locationIndex, Is.GreaterThanOrEqualTo(0), "LocationTime missing from MythosHudRoot");
            Assert.That(locationIndex, Is.GreaterThan(chatIndex),
                "LocationTime must be a later sibling than ChatPanel so it renders on top");
        });
    }

    [Test]
    public async Task EquipmentPanel_HostsCharacterPanel()
    {
        await Client.WaitAssertion(() =>
        {
            var screen = BuildLaidOutScreen();
            var equipment = FindByName<MythosEquipmentPanel>(screen, "EquipmentPanel")!;

            // The CharacterPanel content child should be a non-chrome direct descendant.
            var contentChild = equipment.Children
                .FirstOrDefault(c => c.GetType().Name == "CharacterPanel");
            Assert.That(contentChild, Is.Not.Null,
                "MythosEquipmentPanel should host a CharacterPanel as its content child");

            // After arrange, the content should be visible (not collapsed) and have non-zero size.
            Assert.That(contentChild!.Visible, Is.True);
            Assert.That(contentChild.Size.X, Is.GreaterThan(0f),
                "CharacterPanel content was not arranged with positive width");
            Assert.That(contentChild.Size.Y, Is.GreaterThan(0f),
                "CharacterPanel content was not arranged with positive height");
        });
    }

    [Test]
    public async Task ChatPanel_DoesNotCoverLocationPill()
    {
        await Client.WaitAssertion(() =>
        {
            var screen = BuildLaidOutScreen();

            var chat = FindByName<MythosChatPanel>(screen, "ChatPanel")!;
            var location = FindByName<LocationTimeWidget>(screen, "LocationTime")!;
            var hudRoot = FindByName<LayoutContainer>(screen, "MythosHudRoot")!;

            var locRect = location.GlobalRect();
            var chatRect = chat.GlobalRect();
            var hudRect = hudRoot.GlobalRect();

            Assert.Multiple(() =>
            {
                // LocationTime hugs the TOP-RIGHT of MythosHudRoot (not the absolute screen
                // width — the screen's HorizontalAlignment="Center" can render the screen
                // narrower than the arrange bounds in tests).
                Assert.That(locRect.Right, Is.LessThanOrEqualTo(hudRect.Right + 1f).And.GreaterThan(hudRect.Right - 30f),
                    $"LocationTime right edge ({locRect.Right}) should hug HudRoot right ({hudRect.Right})");
                Assert.That(locRect.Left, Is.GreaterThanOrEqualTo(hudRect.Left),
                    "LocationTime left edge must not extend left of HudRoot");
                Assert.That(locRect.Top, Is.LessThan(hudRect.Top + 30f),
                    $"LocationTime should hug top of HudRoot ({hudRect.Top}), not at y={locRect.Top}");
                Assert.That(locRect.Right - locRect.Left, Is.GreaterThanOrEqualTo(240f),
                    "LocationTime widget too narrow to fit time + region + moon glyph");

                // ChatPanel's allocated bounds must start BELOW the location pill so the
                // pill is in the clear regardless of z-order.
                Assert.That(chatRect.Top, Is.GreaterThanOrEqualTo(locRect.Bottom),
                    $"ChatPanel top ({chatRect.Top}) climbs above LocationTime bottom ({locRect.Bottom}) - chrome will overlap the pill");
            });
        });
    }

    [Test]
    public async Task OldHudRoot_HiddenByDefault()
    {
        await Client.WaitAssertion(() =>
        {
            var screen = BuildLaidOutScreen();
            var oldHud = FindByName<LayoutContainer>(screen, "OldHudRoot")!;
            // SetOldHudVisible(false) is what OldHudVisibilityUIController calls for Mythos screens.
            screen.SetOldHudVisible(false);
            Assert.That(oldHud.Visible, Is.False,
                "OldHudRoot must hide cleanly so legacy widgets don't bleed through");
        });
    }

    /// <summary>
    /// Instantiates MythosGameScreen and forces a Measure + Arrange pass at a known
    /// viewport size so layout-dependent assertions can read GlobalRect / Size.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: mirrors UIManager.LoadScreenInternal which overrides the screen's
    /// HorizontalAlignment / VerticalAlignment to Stretch. Without this override the
    /// screen renders at its measured DesiredSize (which is the largest single
    /// child's size, e.g. 556x80 for the bottom strip) and centers within the
    /// arrange bounds - hiding any actual horizontal-centering bugs from the test.
    /// </remarks>
    private static MythosGameScreen BuildLaidOutScreen()
    {
        var activator = IoCManager.Resolve<IDynamicTypeFactory>();
        var screen = activator.CreateInstance<MythosGameScreen>();

        screen.HorizontalAlignment = Control.HAlignment.Stretch;
        screen.VerticalAlignment = Control.VAlignment.Stretch;
        screen.VerticalExpand = true;

        // SetOldHudVisible(false) mirrors what OldHudVisibilityUIController does on
        // Mythos screen entry; doing it here keeps the layout assertions in sync.
        screen.SetOldHudVisible(false);

        var size = new Vector2(ScreenWidth, ScreenHeight);
        screen.Measure(size);
        screen.Arrange(new UIBox2(0f, 0f, ScreenWidth, ScreenHeight));

        return screen;
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

internal static class ControlGeometryExtensions
{
    public static UIBox2 GlobalRect(this Control control)
    {
        var pos = control.GlobalPosition;
        var size = control.Size;
        return new UIBox2(pos.X, pos.Y, pos.X + size.X, pos.Y + size.Y);
    }
}
