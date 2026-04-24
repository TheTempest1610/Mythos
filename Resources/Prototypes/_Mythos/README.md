# Resources/Prototypes/_Mythos

All Mythos-fork YAML prototypes live here. Mirror the upstream shape: `_Mythos/Actions/`, `_Mythos/Entities/Objects/Weapons/Magic/`, etc.

**Overriding upstream prototypes:** SS14 resolves prototypes by ID, not path. Create an override file under `_Mythos/` with the same prototype ID and it will win (last-loaded). No need to edit upstream YAML.

**New prototypes:** use Mythos-specific IDs where practical (e.g., `MythosMagicMissile`, `MythosWand`) to avoid accidental collisions with upstream content that may reappear via merge.
