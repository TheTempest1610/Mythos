#nullable enable
using Content.Client._Mythos.UserInterface.Screens;
using Content.Client._Mythos.UserInterface.Stylesheets;
using Content.Client._Mythos.UserInterface.Systems.Location.Widgets;
using Content.Client._Mythos.UserInterface.Systems.Vitals.Widgets;
using Content.IntegrationTests.Fixtures;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests.UserInterface;

/// <summary>
/// Phase 2 acceptance tests: HUD stylesheets, vitals orbs strip, and location pill widget.
/// Single fixture batches all assertions to keep the pooled pair reused.
/// </summary>
[TestFixture]
public sealed class MythosHudPhase2Test : GameTest
{
    [Test]
    public async Task Phase2_FrameVitalsLocation()
    {
        await Client.WaitAssertion(() =>
        {
            var activator = IoCManager.Resolve<IDynamicTypeFactory>();
            var screen = activator.CreateInstance<MythosGameScreen>();

            Assert.Multiple(() =>
            {
                // 1. Vitals strip mounted with both circular orbs.
                var vitals = FindByName<VitalsOrbsBar>(screen, "Vitals");
                Assert.That(vitals, Is.Not.Null, "VitalsOrbsBar missing");

                var hpOrb = FindByName<Content.Client._Mythos.UserInterface.Systems.Vitals.Controls.OrbControl>(vitals!, "HpOrb");
                var qiOrb = FindByName<Content.Client._Mythos.UserInterface.Systems.Vitals.Controls.OrbControl>(vitals!, "QiOrb");
                Assert.That(hpOrb, Is.Not.Null, "HpOrb missing");
                Assert.That(qiOrb, Is.Not.Null, "QiOrb missing");

                // 2. Vitals widget responds to SetHp / SetQi (mock data path).
                vitals!.SetHp(1284f, 1284f);
                vitals.SetQi(756f, 812f);
                Assert.That(hpOrb!.Value, Is.EqualTo(1284f).Within(0.001f));
                Assert.That(hpOrb.MaxValue, Is.EqualTo(1284f).Within(0.001f));
                Assert.That(qiOrb!.Value, Is.EqualTo(756f).Within(0.001f));
                Assert.That(qiOrb.MaxValue, Is.EqualTo(812f).Within(0.001f));

                // 3. Orbs carry the Mythos cyan / moon-white palette colors.
                Assert.That(hpOrb.FillColor, Is.EqualTo(MythosPalette.HpFill));
                Assert.That(qiOrb.FillColor, Is.EqualTo(MythosPalette.QiFill));

                // 4. Location pill mounted with the right labels.
                var loc = FindByName<LocationTimeWidget>(screen, "LocationTime");
                Assert.That(loc, Is.Not.Null, "LocationTimeWidget missing");

                loc!.SetLocation("23:42", "Peach Blossom Valley", "☽");
                var time = FindByName<Label>(loc, "TimeLabel");
                var region = FindByName<Label>(loc, "RegionLabel");
                var moon = FindByName<Label>(loc, "MoonGlyph");
                Assert.That(time?.Text, Is.EqualTo("23:42"));
                Assert.That(region?.Text, Is.EqualTo("Peach Blossom Valley"));
                Assert.That(moon?.Text, Is.EqualTo("☽"));

                // 5. Stylesheet rules registered (the rules ship with NanotrasenStylesheet).
                var ui = IoCManager.Resolve<IUserInterfaceManager>();
                var rules = ui.Stylesheet?.Rules;
                Assert.That(rules, Is.Not.Null, "Active stylesheet has no rules");
                Assert.That(rules!, Is.Not.Empty, "Active stylesheet rules empty");
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
