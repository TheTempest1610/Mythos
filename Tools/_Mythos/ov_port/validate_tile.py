"""
Validate a single OV-derived tile prototype end-to-end.

Checks:
  1. The tile YAML parses and contains the target id.
  2. `sprite:` path resolves to an existing PNG under Resources/.
  3. PNG pixel width == variants * size_x (from the RSI's meta.json).
  4. `placementVariants` array length == variants.
  5. `parent:` id is defined in some Tiles/*.yml under Resources/Prototypes/.
  6. `name:` locale key is defined in some en-US/**/*.ftl file.

Exits 0 on all checks passing, non-zero otherwise.

Usage:
  python validate_tile.py <tile_id> [--proto-dir <path>] [--repo <path>]
"""
import argparse
import json
import re
import sys
from pathlib import Path

import yaml
from PIL import Image


# Engine hard constraint: ClydeTileDefinitionManager checks
#   image.Width == tileSize * variants && image.Height == tileSize
# where tileSize = EyeManager.PixelsPerMeter = 32.
ENGINE_TILE_SIZE = 32


def load_all_tiles(proto_root: Path):
    """Yield (file, prototype_dict) for every '- type: tile' entry."""
    for yml in proto_root.rglob("*.yml"):
        try:
            for doc in yaml.safe_load_all(yml.read_text(encoding="utf-8")):
                if not isinstance(doc, list):
                    continue
                for entry in doc:
                    if isinstance(entry, dict) and entry.get("type") == "tile":
                        yield yml, entry
        except yaml.YAMLError as e:
            print(f"[warn] could not parse {yml}: {e}", file=sys.stderr)


def resolve_resource_path(sprite_ref: str, repo: Path) -> Path:
    # Sprite refs look like /Textures/foo/bar.png, rooted at Resources/.
    rel = sprite_ref.lstrip("/")
    return repo / "Resources" / rel


def check_tile(tile: dict, tile_file: Path, repo: Path, all_tiles: list, locale_keys: set):
    checks = []

    tile_id = tile.get("id")
    sprite = tile.get("sprite")
    variants = tile.get("variants", 1)
    placement = tile.get("placementVariants", [])
    parent = tile.get("parent")
    name_key = tile.get("name")

    # 1. Sprite PNG exists
    sprite_path = resolve_resource_path(sprite, repo)
    if sprite_path.exists():
        checks.append(("sprite file exists", True, str(sprite_path)))
    else:
        checks.append(("sprite file exists", False, f"missing: {sprite_path}"))
        return checks  # Can't continue size check without the file.

    # 2. Engine constraint: PNG width == tileSize * variants AND height == tileSize,
    #    where tileSize is hard-coded to 32 in RobustToolbox's tile loader.
    img = Image.open(sprite_path)
    iw, ih = img.size
    expected_w = ENGINE_TILE_SIZE * variants
    ok = iw == expected_w and ih == ENGINE_TILE_SIZE
    detail = f"png={iw}x{ih}, required {expected_w}x{ENGINE_TILE_SIZE} (32x32 cells x {variants} variants)"
    checks.append(("engine tile dims", ok, detail))

    # 3. placementVariants length == variants
    ok = len(placement) == variants
    checks.append(("placementVariants length", ok,
                   f"placementVariants has {len(placement)} entries, variants={variants}"))

    # 4. Parent is defined somewhere
    parent_ok = any(t.get("id") == parent for _f, t in all_tiles) or parent is None
    checks.append(("parent id resolves", parent_ok,
                   f"parent={parent}" if parent_ok else f"parent '{parent}' not found in Prototypes/Tiles/*.yml"))

    # 5. Locale key is defined
    ok = name_key in locale_keys
    checks.append(("locale key defined", ok,
                   f"'{name_key}' found" if ok else f"'{name_key}' missing from any .ftl file"))

    return checks


def collect_locale_keys(locale_root: Path):
    keys = set()
    key_re = re.compile(r"^([a-z][a-z0-9_-]*)\s*=", re.MULTILINE)
    for ftl in locale_root.rglob("*.ftl"):
        for m in key_re.finditer(ftl.read_text(encoding="utf-8", errors="replace")):
            keys.add(m.group(1))
    return keys


def main():
    p = argparse.ArgumentParser()
    p.add_argument("tile_id", nargs="?",
                   help="Tile id to validate. Omit with --batch to audit a directory.")
    p.add_argument("--batch", metavar="PROTO_DIR",
                   help="Validate every tile defined under PROTO_DIR (no tile_id needed).")
    p.add_argument("--repo", default=r"c:/repos/Mythos", help="Mythos repo root")
    args = p.parse_args()

    if not args.tile_id and not args.batch:
        p.error("provide tile_id, or --batch <dir>")

    repo = Path(args.repo)
    proto_root = repo / "Resources" / "Prototypes"
    locale_root = repo / "Resources" / "Locale" / "en-US"

    all_tiles = list(load_all_tiles(proto_root / "Tiles"))
    all_tiles += list(load_all_tiles(proto_root / "_Mythos" / "Tiles"))
    locale_keys = collect_locale_keys(locale_root)

    # Duplicate-id audit (any tile defined twice anywhere is a load-time hazard).
    dup_ids = {}
    for f, t in all_tiles:
        tid = t.get("id")
        dup_ids.setdefault(tid, []).append(f)
    duplicates = {tid: files for tid, files in dup_ids.items() if len(files) > 1}

    if args.batch:
        batch_root = Path(args.batch).resolve()
        batch_tiles = [(f, t) for f, t in all_tiles
                       if f.resolve().is_relative_to(batch_root)]
        if not batch_tiles:
            print(f"no tiles found under {batch_root}", file=sys.stderr)
            return 2
        total_failed = 0
        for f, t in batch_tiles:
            results = check_tile(t, f, repo, all_tiles, locale_keys)
            fails = [(label, detail) for label, ok, detail in results if not ok]
            if fails:
                total_failed += 1
                print(f"FAIL {t['id']} ({f.name}):")
                for label, detail in fails:
                    print(f"  - {label}: {detail}")
        if duplicates:
            print("\nDuplicate tile ids:")
            for tid, files in duplicates.items():
                print(f"  {tid}:")
                for f in files:
                    print(f"    - {f}")
            total_failed += len(duplicates)
        print(f"\n{len(batch_tiles)} tiles checked, {total_failed} failures.")
        return 0 if total_failed == 0 else 1

    matches = [(f, t) for f, t in all_tiles if t.get("id") == args.tile_id]
    if not matches:
        print(f"FAIL: no tile with id '{args.tile_id}' found under {proto_root}", file=sys.stderr)
        return 2
    if len(matches) > 1:
        print(f"FAIL: tile id '{args.tile_id}' is defined in {len(matches)} places:",
              file=sys.stderr)
        for f, _t in matches:
            print(f"  - {f}", file=sys.stderr)
        return 2
    target_file, target = matches[0]

    print(f"Validating tile '{args.tile_id}' from {target_file}\n")
    results = check_tile(target, target_file, repo, all_tiles, locale_keys)

    passed = 0
    failed = 0
    for label, ok, detail in results:
        mark = "PASS" if ok else "FAIL"
        print(f"  [{mark}] {label:32s} - {detail}")
        if ok:
            passed += 1
        else:
            failed += 1

    print(f"\n{passed} passed, {failed} failed")
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
