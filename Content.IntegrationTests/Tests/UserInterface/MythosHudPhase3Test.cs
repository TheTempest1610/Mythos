#nullable enable
using System.Linq;
using Content.Client._Mythos.UserInterface.Screens;
using Content.Client._Mythos.UserInterface.Systems.Character.Controls;
using Content.Client._Mythos.UserInterface.Systems.Character.Widgets;
using Content.Client._Mythos.UserInterface.Systems.Stats;
using Content.Client._Mythos.UserInterface.Systems.Stats.Widgets;
using Content.Client.UserInterface.Controls;
using Content.IntegrationTests.Fixtures;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.IntegrationTests.Tests.UserInterface;

/// <summary>
/// Phase 3 acceptance tests: character sidebar (portrait + 19 equipment slots),
/// stats panel composition, name/class badge, and the MythosCharacterWindow swap.
/// </summary>
[TestFixture]
public sealed class MythosHudPhase3Test : GameTest
{
    [Test]
    public async Task Phase3_CharacterAndStats()
    {
        await Client.WaitAssertion(() =>
        {
            var activator = IoCManager.Resolve<IDynamicTypeFactory>();
            var screen = activator.CreateInstance<MythosGameScreen>();

            Assert.Multiple(() =>
            {
                // 1. Sidebar mounted with portrait + equipment.
                var character = FindByName<CharacterPanel>(screen, "Character");
                Assert.That(character, Is.Not.Null, "CharacterPanel missing");

                var portrait = FindByName<PortraitControl>(character!, "Portrait");
                Assert.That(portrait, Is.Not.Null, "PortraitControl missing");

                var equipment = FindByName<EquipmentSlotsControl>(character!, "EquipmentSlots");
                Assert.That(equipment, Is.Not.Null, "EquipmentSlotsControl missing");

                // 2. Equipment slot count is exactly 19 (V2 spec).
                var slotCount = equipment!.Slots.OfType<SlotButton>().Count();
                Assert.That(slotCount, Is.EqualTo(EquipmentSlotsControl.SlotCount),
                    $"Expected {EquipmentSlotsControl.SlotCount} equipment slots, got {slotCount}");

                // 3. StatsPanel + NameClassBadge instantiate independently with mock data.
                var statsPanel = activator.CreateInstance<StatsPanel>();
                Assert.That(statsPanel, Is.Not.Null);

                var badge = FindByName<NameClassBadge>(statsPanel, "Badge");
                Assert.That(badge, Is.Not.Null, "NameClassBadge missing inside StatsPanel");

                statsPanel.SetCharacter("Test Hero", "Sword Sect Initiate", 12, 4720, 8000);
                statsPanel.SetStats(1284, 756, 142, 88, 211, 167);

                var nameLabel = FindByName<Label>(badge!, "NameLabel");
                var classLabel = FindByName<Label>(badge!, "ClassLabel");
                Assert.That(nameLabel?.Text, Is.EqualTo("Test Hero"));
                Assert.That(classLabel?.Text, Does.Contain("Lv. 12").And.Contain("Sword Sect Initiate"));

                Assert.That(FindByName<Label>(statsPanel, "HpValue")?.Text, Is.EqualTo("1284"));
                Assert.That(FindByName<Label>(statsPanel, "QiValue")?.Text, Is.EqualTo("756"));
                Assert.That(FindByName<Label>(statsPanel, "AtkValue")?.Text, Is.EqualTo("142"));
                Assert.That(FindByName<Label>(statsPanel, "DefValue")?.Text, Is.EqualTo("88"));
                Assert.That(FindByName<Label>(statsPanel, "SpiritValue")?.Text, Is.EqualTo("211"));
                Assert.That(FindByName<Label>(statsPanel, "DexValue")?.Text, Is.EqualTo("167"));

                // 4. StatsUIController.Apply pushes the documented mock values.
                var statsCtrl = IoCManager.Resolve<IUserInterfaceManager>()
                    .GetUIController<StatsUIController>();
                statsCtrl.Apply(statsPanel);
                Assert.That(FindByName<Label>(statsPanel, "HpValue")?.Text,
                    Is.EqualTo($"{StatsUIController.MockHp:F0}"));

                // 5. MythosCharacterWindow inherits CharacterWindow and embeds a StatsPanel.
                var window = activator.CreateInstance<MythosCharacterWindow>();
                Assert.That(window, Is.Not.Null);
                Assert.That(window.Stats, Is.Not.Null,
                    "MythosCharacterWindow.Stats StatsPanel not constructed");

                // Upstream-named widgets must be present (inherited XAML) but hidden.
                Assert.That(window.NameLabel, Is.Not.Null,
                    "Inherited NameLabel must exist for upstream CharacterUpdated path");
                Assert.That(window.NameLabel.Visible, Is.False,
                    "Upstream NameLabel should be hidden in Mythos window");
                Assert.That(window.RoleType.Visible, Is.False);
                Assert.That(window.Objectives.Visible, Is.False);
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
