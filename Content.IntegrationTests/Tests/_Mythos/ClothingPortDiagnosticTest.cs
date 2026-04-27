// Mythos: diagnostic + regression test for the OV clothing port
// inheritance chain.
//
// Lives at Content.IntegrationTests/Tests/_Mythos/. Originally a
// debugging aid for Phase B (chargen Clothing tab) -- the picker filter
// was failing because the prototype-manager parent walk could not
// resolve abstract prototypes. The probe revealed that
// HasIndex/TryIndex/EnumerateParents all return false for abstract
// prototypes (including upstream "Clothing"), even though the abstracts
// are clearly loaded (concrete items inherit components through them).
// The asserts below pin that behavior down so a future SS14 upgrade
// that changes the contract gives us a fast regression signal, and
// document the right semantic filter (component-presence on the
// concrete item) for any future picker.
//
// Run with:
//   dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj
//     --filter "FullyQualifiedName~ClothingPortDiagnosticTest"
using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests.Pair;
using Content.Shared.Clothing.Components;
using NUnit.Framework;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._Mythos;

[TestOf(typeof(EntityPrototype)), FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class ClothingPortDiagnosticTest
{
    // Typed prototype IDs avoid the RA0033 lint ("HasIndex/TryIndex
    // forbid literal values") and double as a contract about which
    // prototypes this test is asserting against.
    private static readonly EntProtoId UpstreamClothingRoot = "Clothing";
    private static readonly EntProtoId UpstreamClothingOuterBase = "ClothingOuterBase";
    private static readonly EntProtoId OvClothingRoot = "OVRoguetownClothingBase";
    private static readonly EntProtoId OvOuterClothingBase = "OVRoguetownOuterClothingBase";
    private static readonly EntProtoId OvOuterArmorBase = "OVRoguetownOuterClothingArmorBase";
    private static readonly EntProtoId OvOuterArmorPlateBase = "OVRoguetownOuterClothingArmorPlateBase";
    private static readonly EntProtoId OvSteelHalfPlate = "OVSteelHalfPlate";

    private TestPair _pair = default!;
    private RobustIntegrationTest.ServerIntegrationInstance Server => _pair.Server;

    [SetUp]
    public async Task SetUp()
    {
        _pair = await PoolManager.GetServerClient(
            new PoolSettings { Connected = false },
            new NUnitTestContextWrap(TestContext.CurrentContext, TestContext.Out));
    }

    [TearDown]
    public async Task TearDown()
    {
        await _pair.CleanReturnAsync();
    }

    /// <summary>
    /// Probes each prototype in the OV clothing inheritance chain and
    /// reports which ones are loaded server-side. Useful as a diagnostic
    /// rather than a strict pass/fail; assertions document the EXPECTED
    /// state once the load problem is fixed.
    /// </summary>
    [Test]
    public async Task ProbeOvInheritanceChain()
    {
        var protoMan = Server.ResolveDependency<IPrototypeManager>();

        // Order matters only for the readout; assertions below are
        // order-independent.
        var probes = new[]
        {
            UpstreamClothingRoot,
            UpstreamClothingOuterBase,
            OvClothingRoot,
            OvOuterClothingBase,
            OvOuterArmorBase,
            OvOuterArmorPlateBase,
            OvSteelHalfPlate,
        };

        await Server.WaitPost(() =>
        {
            foreach (var id in probes)
            {
                var present = protoMan.HasIndex(id);
                TestContext.Progress.WriteLine($"PROBE {id.Id}: present={present}");
            }

            // For OVSteelHalfPlate (the item): list its declared parents and
            // the FULL set of components it ended up with. If
            // MythosClothing is missing from the components dict,
            // _root.yml didn't load and the multi-parent chain silently
            // dropped the Mythos root.
            if (protoMan.TryIndex(OvSteelHalfPlate, out var item))
            {
                var parents = item.Parents == null
                    ? "<none>"
                    : string.Join(", ", item.Parents);
                TestContext.Progress.WriteLine(
                    $"OVSteelHalfPlate.Parents = [{parents}]");
                TestContext.Progress.WriteLine(
                    $"OVSteelHalfPlate.Components keys = "
                    + $"[{string.Join(", ", item.Components.Keys.OrderBy(k => k))}]");
            }

            // Also probe via TryIndex for a known abstract directly to
            // confirm whether abstract prototypes are reachable via
            // TryIndex (they're not -- this is the SS14 quirk).
            if (protoMan.TryIndex(OvOuterClothingBase, out var slotBase))
            {
                var slotParents = slotBase.Parents == null
                    ? "<none>"
                    : string.Join(", ", slotBase.Parents);
                TestContext.Progress.WriteLine(
                    $"OVRoguetownOuterClothingBase.Parents = [{slotParents}] "
                    + $"abstract={slotBase.Abstract}");
            }
        });

        await Server.WaitAssertion(() =>
        {
            // (1) SS14 quirk: HasIndex / TryIndex return false for
            // ABSTRACT prototypes, even when those abstracts clearly
            // load (their components cascade into concrete descendants).
            // We pin this down so a future SS14 upgrade that changes
            // the contract gives a fast regression signal -- if either
            // of these starts returning true, the picker filter could
            // be simplified to walk parents.
            Assert.That(protoMan.HasIndex(UpstreamClothingRoot),
                Is.False,
                "SS14 used to surface upstream abstract 'Clothing' as "
                + "false via HasIndex. If this changes, revisit "
                + "MythosClothingPicker -- the parent-walk filter "
                + "could become the cleaner choice over the "
                + "MythosClothing-component marker filter.");
            Assert.That(protoMan.HasIndex(OvClothingRoot),
                Is.False,
                "Same SS14 quirk applies to our manual root abstract.");

            // (2) Concrete items DO load and ARE indexable.
            Assert.That(protoMan.HasIndex(OvSteelHalfPlate),
                Is.True, "OVSteelHalfPlate concrete item must load.");
            Assert.That(protoMan.TryIndex(OvSteelHalfPlate, out var item),
                Is.True);

            // (3) Despite (1), the inheritance chain works: items
            // inherit MythosClothing transitively from
            // OVRoguetownClothingBase. This is the signal the picker
            // uses; if it ever fails, items would lose sex/species
            // sprite resolution silently.
            Assert.That(item!.Components.ContainsKey("MythosClothing"),
                Is.True,
                "OVSteelHalfPlate must inherit MythosClothing component "
                + "from OVRoguetownClothingBase. If false, the multi-parent "
                + "chain dropped the Mythos root silently.");
            Assert.That(item.Components.ContainsKey("Clothing"),
                Is.True,
                "OVSteelHalfPlate must inherit Clothing component from "
                + "upstream ClothingOuterBase via OVRoguetownOuterClothingBase.");
        });
    }
}
