"""
Generate SS14 tile sprites + tile prototypes from Ochre-Valley DMI turf files.

For each DMI:
  - Groups icon_states by base name (trailing digits stripped) to form variant sets.
  - Concatenates variants horizontally into a <base>.png strip inside a .rsi folder.
  - Writes meta.json and a _manifest.json sidecar recording provenance
    (source DMI + source state name for each variant) so diff_tiles.py can
    verify the output pixel-for-pixel against the DMI ground truth.
  - Emits a tile prototype YAML (minimal, user should refine parent/name).

Usage:
  python mktiles.py <dmi_or_dir> <rsi_output_root> <proto_output_dir>
      [--source-label "..."]
"""
import argparse
import json
import math
import re
import sys
from collections import OrderedDict
from pathlib import Path

from PIL import Image

from dmi2rsi import load_dmi, safe_state_filename


# Skip these state-name patterns (debug / placeholder / UI).
BLACKLIST_PATTERNS = [
    re.compile(r"^$"),           # BYOND's anonymous default state, not a usable tile.
    re.compile(r"^_+$"),
    re.compile(r"cantfind", re.I),
    re.compile(r"example", re.I),
    re.compile(r"noop", re.I),
    re.compile(r"^debug", re.I),
]

VARIANT_RE = re.compile(r"^(.+?)[\-_]?(\d+)$")

# Direction-suffix patterns. BYOND smooths produce per-direction tiles in
# two naming conventions:
#   <base>-<dir>       e.g. cobblerock-n, cobblerock-se
#   <base>edge-<dir>   e.g. dirtedge-n, grass_coldedge-w
# SS14 handles edges via `edgeSprites:` on a single parent tile; we fold these
# into the parent if the parent exists as a standalone tile.
EDGE_DIR_RE = re.compile(r"^(.+?)edge-(n|s|e|w|ne|nw|se|sw)$", re.I)
DIR_RE = re.compile(r"^(.+?)-(n|s|e|w|ne|nw|se|sw)$", re.I)

# A directional sprite that's mostly opaque isn't an edge fragment, it's a
# full tile that BYOND happened to name with a direction suffix (cobblerock-n,
# cobblerock-se etc are full cobble patterns, not edges). Threshold picked from
# observed coverage: real edges sit at 9-18%, full tiles at 100%, with thick
# patterns like oldcobblerock straddling the line at ~73%.
EDGE_ALPHA_THRESHOLD = 0.70


def is_edge_sprite(png_path: Path) -> bool:
    img = Image.open(png_path).convert("RGBA")
    total = img.width * img.height
    opaque = sum(1 for px in img.getdata() if px[3] > 0)
    return (opaque / total) < EDGE_ALPHA_THRESHOLD
SS14_DIR_NAMES = {
    "n": "North", "s": "South", "e": "East", "w": "West",
    "ne": "NorthEast", "nw": "NorthWest", "se": "SouthEast", "sw": "SouthWest",
}
CARDINALS = ("s", "e", "n", "w")          # canonical = "s"
CORNERS = ("se", "ne", "nw", "sw")        # canonical = "se"


def is_blacklisted(name: str) -> bool:
    return any(p.search(name) for p in BLACKLIST_PATTERNS)


def group_variants(state_names):
    """Group state names into {base_name: [state_name_in_variant_order]}.

    Strategy:
      - 'sand', 'sand1', 'sand2', ..., 'sand9'  -> {'sand': ['sand','sand1',...]}
      - 'snow0', 'snow1', ..., 'snow12' with no 'snow' base -> {'snow': ['snow0','snow1',...]}
      - Names not matching a numeric pattern become their own group.
      - Ordering within a group: unnumbered first, then numeric ascending.
    """
    groups = OrderedDict()
    unnumbered = {}
    numbered = {}

    for name in state_names:
        if is_blacklisted(name):
            continue
        m = VARIANT_RE.match(name)
        if m:
            base = m.group(1)
            numbered.setdefault(base, []).append((int(m.group(2)), name))
        else:
            unnumbered[name] = name

    # Merge: unnumbered 'foo' + numbered 'foo' group
    seen_bases = set()
    for name in state_names:
        if is_blacklisted(name) or name in seen_bases:
            continue
        m = VARIANT_RE.match(name)
        base = m.group(1) if m else name
        if base in seen_bases:
            continue
        seen_bases.add(base)

        variants = []
        # Unnumbered base first, if present.
        if base in unnumbered and base not in {v for v in variants}:
            variants.append(base)
        # Then numbered variants ascending.
        if base in numbered:
            for _n, vname in sorted(numbered[base], key=lambda x: x[0]):
                variants.append(vname)
        if not variants:
            # Plain name, no digits, no unnumbered base listed yet (shouldn't happen).
            variants = [name]
        groups[base] = variants

    return groups


def first_cell(cells):
    """Pick the first visually-representative cell: dir0 frame0 = cells[0]."""
    return cells[0]


def build_variant_strip(variant_cells, size_x, size_y):
    """Horizontal strip: variants * size_x wide, size_y tall."""
    n = len(variant_cells)
    strip = Image.new("RGBA", (n * size_x, size_y), (0, 0, 0, 0))
    for i, c in enumerate(variant_cells):
        strip.paste(c, (i * size_x, 0))
    return strip


def slugify(s: str) -> str:
    """Lowercase, strip non-alnum-or-dash-or-underscore for localization slugs."""
    return re.sub(r"[^a-z0-9_\-]+", "-", s.lower()).strip("-") or "unnamed"


def pascal(s: str) -> str:
    return "".join(p.capitalize() for p in re.split(r"[^a-zA-Z0-9]+", s) if p) or "Unnamed"


TILE_CELL_SIZE = 32  # SS14 hard constraint: EyeManager.PixelsPerMeter == 32.


def relative_source_path(dmi_path: Path, ov_root: Path | None) -> str:
    """Path to record in the manifest. Strips parent-folder by storing
    the path relative to ``ov_root`` if given; otherwise just the basename.
    Never embeds an absolute filesystem path."""
    if ov_root is not None:
        try:
            return str(dmi_path.resolve().relative_to(ov_root.resolve())).replace("\\", "/")
        except ValueError:
            pass
    return dmi_path.name


def process_dmi(dmi_path: Path, rsi_out_root: Path, proto_out_dir: Path,
                source_label: str | None = None, license_: str | None = None,
                ov_root: Path | None = None):
    width, height, states, cells_by_state = load_dmi(dmi_path)
    if not states:
        print(f"[skip] {dmi_path.name} (no states)")
        return None

    if width != TILE_CELL_SIZE or height != TILE_CELL_SIZE:
        # Engine rejects non-32x32 tile textures (ClydeTileDefinitionManager).
        # Sprites can still be ported via dmi2rsi.py and used as entity sprites,
        # they just can't be registered as tile prototypes.
        print(f"[skip] {dmi_path.name} ({width}x{height} cells; tiles require 32x32)")
        return None

    category = dmi_path.stem  # e.g. "grass", "floors"
    # Flat PNG layout (no .rsi wrapper): tiles don't use RSI state machinery,
    # and the engine warns "Loading raw texture inside RSI" for PNGs under .rsi/.
    out_dir = rsi_out_root / category
    out_dir.mkdir(parents=True, exist_ok=True)

    # Deduplicate by state name (BYOND allows duplicates; first one wins).
    # Without this, dict-indexing picks the last occurrence while diff_tiles'
    # linear scan picks the first -> silent mismatch.
    seen = set()
    dedup_states = []
    for i, s in enumerate(states):
        if s["name"] in seen:
            continue
        seen.add(s["name"])
        dedup_states.append((i, s["name"]))
    state_names = [n for _, n in dedup_states]
    state_index = {n: i for i, n in dedup_states}
    groups = group_variants(state_names)

    if not groups:
        print(f"[skip] {dmi_path.name} (all states blacklisted)")
        return None

    source_dmi_rel = relative_source_path(dmi_path, ov_root)

    manifest_tiles = []
    proto_entries = []
    used_fnames = set()

    for base, variant_state_names in groups.items():
        fname = safe_state_filename(base, used_fnames)
        # Build strip from first cell of each variant state.
        variant_cells = [first_cell(cells_by_state[state_index[v]]) for v in variant_state_names]
        strip = build_variant_strip(variant_cells, width, height)
        strip.save(out_dir / f"{fname}.png")

        manifest_tiles.append({
            "tile_id": fname,
            "png": f"{fname}.png",
            "variants": [
                {
                    "index": i,
                    "source_dmi": source_dmi_rel,
                    "source_state": vname,
                } for i, vname in enumerate(variant_state_names)
            ],
        })

        n_variants = len(variant_state_names)
        proto_id = f"FloorOV{pascal(category)}{pascal(base)}"
        proto_entries.append({
            "id": proto_id,
            "base": base,
            "fname": fname,
            "name_slug": f"tiles-ov-{slugify(category)}-{slugify(base)}",
            "sprite": f"/Textures/Tiles/_Mythos/_OV/{category}/{fname}.png",
            "variants": n_variants,
        })

    # Write _manifest.json (sidecar, consumed by diff_tiles.py).
    # No meta.json: tiles aren't RSIs and don't need the state machinery.
    # License/copyright deliberately not auto-populated; caller can ignore or
    # pass --license / --source-label if accurate values are known.
    manifest = {
        "source_dmi": source_dmi_rel,
        "size": {"x": width, "y": height},
        "tiles": manifest_tiles,
    }
    if license_:
        manifest["license"] = license_
    if source_label:
        manifest["copyright"] = source_label
    with open(out_dir / "_manifest.json", "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2)
        f.write("\n")

    # ----- Direction-suffix fold -----
    # Detect <base>-<dir> tiles whose <base> exists as a non-directional tile.
    # Fold them into the parent's edgeSprites:; drop them as standalone tiles.
    # Engine auto-rotates from canonical (-s for cardinals, -se for corners),
    # so referencing the parent's directional PNGs through edgeSprites uses
    # ONLY the canonical files; referencing BYOND's pre-rotated -n/-e/-w/-ne
    # would double-rotate them at atlas-build time.
    bases = {e["base"]: e for e in proto_entries}
    edges_by_parent: dict[str, dict[str, str]] = {}
    folded_directionals: set[str] = set()
    folded_originals: dict[tuple[str, str], str] = {}  # (parent, dir) -> e["base"]
    full_tile_kept: list[str] = []
    for e in proto_entries:
        # Prefer <base>edge-<dir> over <base>-<dir>: both require the resolved
        # parent name to exist as a real standalone tile, so the more specific
        # match wins when both apply, and ambiguous strips that resolve to a
        # non-existent parent (e.g. "lavedge-n" -> "lav") fall through.
        parent_base = None
        dir_short = None
        for regex in (EDGE_DIR_RE, DIR_RE):
            m = regex.match(e["base"])
            if m and m.group(1) in bases:
                parent_base, dir_short = m.group(1), m.group(2).lower()
                break
        if parent_base is None:
            continue
        # Alpha gate: BYOND sometimes names full-coverage tile variants with
        # direction suffixes (cobblerock-n etc are 100% opaque alternate
        # patterns, not edge fragments). Skip those; leave as standalone.
        if not is_edge_sprite(out_dir / f"{e['fname']}.png"):
            full_tile_kept.append(e["base"])
            continue
        edges_by_parent.setdefault(parent_base, {})[dir_short] = e["fname"]
        folded_directionals.add(e["base"])
        folded_originals[(parent_base, dir_short)] = e["base"]

    if full_tile_kept:
        head = ", ".join(full_tile_kept[:6])
        tail = f" (+{len(full_tile_kept)-6})" if len(full_tile_kept) > 6 else ""
        print(f"[fulltile] {category}: directional names with >= "
              f"{int(EDGE_ALPHA_THRESHOLD*100)}% opaque coverage kept as standalone tiles: "
              f"{head}{tail}")

    # Pick canonical PNG per parent. BYOND -n already has art at the top of
    # the image, matching SS14's canonical "South" orientation (drawn at the
    # cell south of source, art rendered at the boundary = top of dest cell).
    # Same logic: BYOND -nw maps to canonical "SouthEast".
    # If neither canonical exists, skip the fold so we don't reference a
    # non-canonical sprite that the engine would render with wrong rotation.
    edge_blocks: dict[str, dict[str, str]] = {}
    for parent_base, dirs in edges_by_parent.items():
        cardinal_canon = dirs.get("n")
        corner_canon = dirs.get("nw")
        if not cardinal_canon and not corner_canon:
            for dir_short in dirs:
                orig = folded_originals.pop((parent_base, dir_short), None)
                if orig:
                    folded_directionals.discard(orig)
            continue
        block = {}
        cat_path = f"/Textures/Tiles/_Mythos/_OV/{category}"
        if corner_canon:
            for ss14 in ("SouthEast", "NorthEast", "NorthWest", "SouthWest"):
                block[ss14] = f"{cat_path}/{corner_canon}.png"
        if cardinal_canon:
            for ss14 in ("South", "East", "North", "West"):
                block[ss14] = f"{cat_path}/{cardinal_canon}.png"
        edge_blocks[parent_base] = block

        non_canonical = [d for d in dirs if d not in ("n", "nw")]
        if non_canonical:
            print(f"[edge] {category}/{parent_base}: BYOND had per-direction art "
                  f"({','.join(sorted(non_canonical))}); using canonical only "
                  f"({'-n' if cardinal_canon else ''}{' -nw' if corner_canon else ''}). "
                  f"Review for asymmetry.")

    # Drop directional entries whose parent absorbed them.
    def _absorbed(entry):
        if entry["base"] not in folded_directionals:
            return False
        for regex in (EDGE_DIR_RE, DIR_RE):
            m = regex.match(entry["base"])
            if m and m.group(1) in edge_blocks:
                return True
        return False

    proto_entries = [e for e in proto_entries if not _absorbed(e)]

    # Append tile prototype YAML
    proto_out_dir.mkdir(parents=True, exist_ok=True)
    proto_path = proto_out_dir / f"{category}.yml"
    with open(proto_path, "w", encoding="utf-8") as f:
        f.write(f"# Auto-generated by Tools/_Mythos/ov_port/mktiles.py\n")
        f.write(f"# Source: {dmi_path.name}\n")
        f.write(f"# Review parent / name / itemDrop / footstepSounds before using in-game.\n\n")
        for e in proto_entries:
            f.write(f"- type: tile\n")
            f.write(f"  id: {e['id']}\n")
            f.write(f"  parent: BaseStationTile\n")
            f.write(f"  name: {e['name_slug']}\n")
            f.write(f"  sprite: {e['sprite']}\n")
            f.write(f"  variants: {e['variants']}\n")
            f.write(f"  placementVariants:\n")
            for _ in range(e["variants"]):
                f.write(f"  - 1.0\n")
            if e["base"] in edge_blocks:
                f.write(f"  edgeSpritePriority: 1\n")
                f.write(f"  edgeSprites:\n")
                for ss14_dir, png_path in edge_blocks[e["base"]].items():
                    f.write(f"    {ss14_dir}: {png_path}\n")
            f.write(f"\n")

    print(f"[ok]   {dmi_path.name} -> {category}.rsi ({len(groups)} tiles, {sum(len(v) for v in groups.values())} variants)")
    return category


def main():
    p = argparse.ArgumentParser()
    p.add_argument("input", help="DMI file or directory of DMIs")
    p.add_argument("rsi_out_root", help="Output root for tile .rsi folders (e.g. Resources/Textures/Tiles/_Mythos/_OV)")
    p.add_argument("proto_out_dir", help="Output directory for tile prototype YAMLs (e.g. Resources/Prototypes/_Mythos/Tiles/_OV)")
    p.add_argument("--source-label", default=None,
                   help="Optional. If omitted, no copyright field is emitted to meta.json.")
    p.add_argument("--license", default=None,
                   help="Optional. If omitted, no license field is emitted to meta.json.")
    p.add_argument("--ov-root", default=None,
                   help="OV repo root. If given, manifest source_dmi paths are stored "
                        "relative to it (e.g. 'icons/turf/grass.dmi'); otherwise just the "
                        "filename is stored.")
    args = p.parse_args()

    inp = Path(args.input)
    rsi_root = Path(args.rsi_out_root)
    proto_dir = Path(args.proto_out_dir)
    ov_root = Path(args.ov_root) if args.ov_root else None

    if inp.is_file():
        process_dmi(inp, rsi_root, proto_dir, args.source_label, args.license, ov_root)
    else:
        ok = 0
        for dmi in sorted(inp.rglob("*.dmi")):
            try:
                if process_dmi(dmi, rsi_root, proto_dir, args.source_label, args.license, ov_root):
                    ok += 1
            except Exception as e:
                print(f"[fail] {dmi.name}: {e}", file=sys.stderr)
        print(f"\nProcessed {ok} DMI -> tile sets.")


if __name__ == "__main__":
    main()
