# Content.Tests/_Mythos

Unit tests (NUnit, no engine spin-up) for all Mythos-fork code. Mirror the source shape: `_Mythos/Combat/Queue/CombatQueueValidationTests.cs`, etc.

**Base class:** `ContentUnitTest` for tests that need IoC-registered content assemblies.

**Category tags:** tag each test with `[Category("Mythos")]` so CI can run the whole Mythos suite via `dotnet test --filter Category=Mythos`.
