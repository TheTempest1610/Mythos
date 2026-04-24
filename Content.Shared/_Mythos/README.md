# Content.Shared/_Mythos

All Mythos-fork shared code (components, events, shared systems) lives under this directory. Mirror the upstream shape: `_Mythos/Combat/Queue/`, `_Mythos/Magic/Mana/`, etc.

**Namespace:** `Content.Shared.Mythos.*` (no underscore in the namespace). RobustToolbox's component/system reflection scan finds types anywhere in the assembly; the namespace is cosmetic.

**Touching upstream files (preference order):**
1. Event hook: raise a cancellable `[ByRefEvent]` at the top of the upstream method, subscribe from a Mythos system. Upstream edit should be ~3 lines.
2. Partial class: declare the upstream class `partial`, add Mythos methods in a `*.Mythos.cs` sibling file under `_Mythos/`.
3. Inline edit: only when 1 and 2 don't fit. Tag every touch with `// Mythos: <one-line reason>`.

At upstream-merge time, `grep -rn "// Mythos:"` discovers every touch-point.
