# Testing and Verification

## Purpose

Use this page before changing behavior that is already covered by Mythos tests. The goal is to keep fork gameplay contracts explicit while avoiding broad, slow test runs when a focused test is enough.

## Source Anchors

- `Content.Tests/_Mythos/`
- `Content.IntegrationTests/_Mythos/`
- `Content.Tests/_Mythos/README.md`
- `Content.IntegrationTests/_Mythos/README.md`
- `Content.Tests/Content.Tests.csproj`
- `Content.IntegrationTests/Content.IntegrationTests.csproj`

## Test Layers

| Layer | Folder | Use for |
| --- | --- | --- |
| Unit tests | `Content.Tests/_Mythos/` | Pure functions, default component values, simple contracts that do not need a client/server pair |
| Integration tests | `Content.IntegrationTests/_Mythos/` | ECS systems, network replication, player attach, actions, DoAfter, damage, queue flow |

All Mythos tests should use:

```csharp
[Category("Mythos")]
```

## Current Coverage Map

| Area | Unit tests | Integration tests |
| --- | --- | --- |
| Mana defaults/math/refund | `Content.Tests/_Mythos/Magic/Mana/` | `Content.IntegrationTests/_Mythos/Magic/Mana/ManaSystemTests.cs` |
| Combat target defaults | `Content.Tests/_Mythos/Combat/Targeting/CombatTargetComponentDefaultsTests.cs` | `Content.IntegrationTests/_Mythos/Combat/Targeting/TargetSelectionTests.cs` |
| Combat queue defaults | `Content.Tests/_Mythos/Combat/Queue/` | `Content.IntegrationTests/_Mythos/Combat/Queue/CombatQueueTests.cs` |
| Magic Missile queue behavior | n/a | `Content.IntegrationTests/_Mythos/Magic/MagicMissile/MagicMissileQueueTests.cs` |
| Fireball cast lifecycle | n/a | `Content.IntegrationTests/_Mythos/Magic/Fireball/FireballCastTests.cs` |
| Player spell loadout | n/a | `Content.IntegrationTests/_Mythos/Player/MythosSpellLoadoutTests.cs` |

## Commands

Fast unit suite:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --filter Category=Mythos --no-restore
```

Integration suite:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter Category=Mythos --no-restore
```

Focused test by class:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~FireballCastTests" --no-restore
```

Build client after UI/shared changes:

```powershell
dotnet build Content.Client/Content.Client.csproj --no-restore
```

## When to Add Tests

Add or update tests when changing:

- `ManaComponent` fields or `SharedManaSystem` math
- queue slot identity, capacity, global cooldown, pop/cancel behavior
- target validation rules
- spell cost, cast time, damage type, or DoAfter cancellation behavior
- `MythosSpellLoadoutSystem` grants
- networked component fields or event payloads
- server authority around damage, mana spending, or cast completion

For pure math, write a unit test. For anything involving `EntityManager`, player attachment, component replication, actions, or DoAfter, write an integration test.

## Agent Search Terms

```powershell
rg --files Content.Tests\_Mythos Content.IntegrationTests\_Mythos
rg -n "Category\\(\"Mythos\"\\)|TestOf|GameTest|InteractionTest|PoolManager|WaitPost|WaitRunTicks" Content.Tests\_Mythos Content.IntegrationTests\_Mythos -g "*.cs"
rg -n "ManaComponent|CombatQueueComponent|CombatTargetComponent|Fireball|MagicMissile|MythosSpellLoadout" Content.Tests\_Mythos Content.IntegrationTests\_Mythos -g "*.cs"
```
