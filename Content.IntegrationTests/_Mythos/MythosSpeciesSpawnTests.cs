using System.Collections.Generic;
using System.Threading.Tasks;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Body;
using Content.Shared.Clothing.Mythos.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests._Mythos
{
    /// <summary>
    /// Track D acceptance: integration test that spawns each V1 species through
    /// the EntityManager and verifies component composition matches the
    /// MobHumenMythosBase parent expectations:
    /// 1. The mob entity prototype loads.
    /// 2. The matching SpeciesPrototype loads with full-RGB Hues coloration and
    ///    is round-start eligible.
    /// 3. The spawned mob carries the Mythos clothing marker, the humanoid
    ///    profile component (matching this fork's HumanoidProfileComponent), and
    ///    a body component (from the BaseSpeciesMobOrganic chain).
    /// 4. The HumanoidProfile species ID matches the species prototype ID.
    ///
    /// Sprite RSI loading is intentionally NOT exercised here: Track E has not
    /// landed yet. The path "_Mythos/Mobs/Species/Humen/parts.rsi" is a known
    /// future-asset reference and the integration harness does not render
    /// frames anyway.
    /// </summary>
    [TestFixture]
    public sealed class MythosSpeciesSpawnTests : GameTest
    {
        /// <summary>Tuples of (SpeciesPrototype id, mob EntityPrototype id).</summary>
        private static readonly IReadOnlyList<(string Species, string Mob)> V1Species = new[]
        {
            ("Humen", "MobHumenMythos"),
            ("Aasimar", "MobAasimarMythos"),
            ("HalfElf", "MobHalfElfMythos"),
            ("Tiefling", "MobTieflingMythos"),
            ("Dullahan", "MobDullahanMythos"),
            ("Demihuman", "MobDemihumanMythos"),
        };

        [Test]
        public async Task AllV1SpeciesPrototypesLoad()
        {
            await Server.WaitIdleAsync();
            await Server.WaitAssertion(() =>
            {
                var protos = Server.ResolveDependency<IPrototypeManager>();
                foreach (var (speciesId, _) in V1Species)
                {
                    Assert.That(protos.HasIndex<SpeciesPrototype>(speciesId), Is.True,
                        $"SpeciesPrototype '{speciesId}' is missing.");
                    var species = protos.Index<SpeciesPrototype>(speciesId);
                    Assert.That(species.RoundStart, Is.True,
                        $"Species '{speciesId}' must be round-start to appear in chargen.");
                    Assert.That((string) species.SkinColoration, Is.EqualTo("Hues"),
                        $"Species '{speciesId}' must use Hues full-RGB skin coloration for V1.");
                }
            });
        }

        [Test]
        public async Task AllV1SpeciesMobPrototypesLoad()
        {
            await Server.WaitIdleAsync();
            await Server.WaitAssertion(() =>
            {
                var protos = Server.ResolveDependency<IPrototypeManager>();
                foreach (var (_, mobId) in V1Species)
                {
                    Assert.That(protos.HasIndex<EntityPrototype>(mobId), Is.True,
                        $"Mob EntityPrototype '{mobId}' is missing.");
                }
            });
        }

        [Test]
        public async Task SpawningEachV1SpeciesAttachesExpectedComponents()
        {
            await Server.WaitIdleAsync();

            foreach (var (speciesId, mobId) in V1Species)
            {
                var uid = await Spawn(mobId);

                await Server.WaitAssertion(() =>
                {
                    var entMan = Server.EntMan;
                    Assert.That(entMan.EntityExists(uid), Is.True,
                        $"Failed to spawn mob '{mobId}'.");

                    Assert.That(entMan.HasComponent<MythosClothingComponent>(uid), Is.True,
                        $"Mob '{mobId}' missing MythosClothingComponent (Track B marker).");
                    Assert.That(entMan.HasComponent<HumanoidProfileComponent>(uid), Is.True,
                        $"Mob '{mobId}' missing HumanoidProfileComponent.");
                    Assert.That(entMan.HasComponent<BodyComponent>(uid), Is.True,
                        $"Mob '{mobId}' missing BodyComponent (from BaseSpeciesMobOrganic).");

                    var profile = entMan.GetComponent<HumanoidProfileComponent>(uid);
                    Assert.That((string) profile.Species, Is.EqualTo(speciesId),
                        $"Mob '{mobId}' HumanoidProfile.species should be '{speciesId}'.");
                });
            }
        }

        private static readonly EntProtoId VanillaMobHuman = "MobHuman";
        private static readonly ProtoId<SpeciesPrototype> VanillaHumanSpecies = "Human";

        [Test]
        public async Task VanillaHumanRemainsSpawnable()
        {
            // Backward-compat regression: Track D must not break vanilla SS14
            // species. MobHuman should still load and spawn.
            await Server.WaitIdleAsync();
            await Server.WaitAssertion(() =>
            {
                var protos = Server.ResolveDependency<IPrototypeManager>();
                Assert.That(protos.HasIndex(VanillaMobHuman), Is.True,
                    "Vanilla MobHuman entity prototype must remain present.");
                Assert.That(protos.HasIndex(VanillaHumanSpecies), Is.True,
                    "Vanilla Human species prototype must remain present.");
                var human = protos.Index(VanillaHumanSpecies);
                Assert.That(human.RoundStart, Is.True,
                    "Vanilla Human must remain round-start eligible.");
            });

            var uid = await Spawn("MobHuman");
            await Server.WaitAssertion(() =>
            {
                Assert.That(Server.EntMan.EntityExists(uid), Is.True,
                    "Vanilla MobHuman must still spawn cleanly.");
            });
        }
    }
}
