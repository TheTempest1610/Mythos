# Content.IntegrationTests/_Mythos

Integration tests for Mythos-fork code. Each test spins up a paired server + client via `TestPair` / `PoolManager`, so both assemblies are built and exercised per test.

**Base class:** `InteractionTest` is a good starting point for UX-style tests; it comes with a spawned player, map, and helper methods. The Mythos test harness at `_Mythos/Combat/CombatTestSystem.cs` extends it with combat-specific helpers (`SelectTarget`, `PressHotbarSlot`, `AssertQueueSlotOccupied`, `TickUntilCooldownElapses`).

**Category tags:** `[Category("Mythos")]` on every fixture so CI can run the whole Mythos suite via `dotnet test --filter Category=Mythos`.

**Regression:** the full Mythos suite must continue to pass when new tests land.
