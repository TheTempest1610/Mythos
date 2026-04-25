"""
Programmatic visual diff between generated SS14 tile sprites and the original
Ochre-Valley DMI cells they were derived from.

Reads _manifest.json sidecars written by mktiles.py (one per generated
tile .rsi), which record source DMI + source icon_state for each variant.
For each variant:
  - Extracts the expected cell from the source DMI (dir0 frame0).
  - Extracts the same cell from the generated tile PNG strip.
  - Computes PIL.ImageChops.difference.
  - Records: (nonzero pixel count, max per-channel delta, bbox of diff).

Mismatches emit a side-by-side diagnostic PNG (source | generated | diff-amplified)
under <rsi_out_root>/_diff/<category>_<tile>_<variant>.png.

Exits 0 iff every variant is pixel-identical.

Usage:
  python diff_tiles.py <rsi_out_root>
"""
import argparse
import json
import sys
from pathlib import Path

from PIL import Image, ImageChops

from dmi2rsi import load_dmi


def _resolve_source(path_str: str, ov_root: Path | None) -> Path:
    """source_dmi may be relative (committed manifests)
    or absolute (legacy). Resolve against --ov-root when relative."""
    p = Path(path_str)
    if p.is_absolute():
        return p
    if ov_root is None:
        raise RuntimeError(
            f"manifest source_dmi is relative ('{path_str}') but --ov-root was not given. "
            f"Pass --ov-root <path-to-Ochre-Valley-checkout>.")
    return ov_root / p


def _cache_load_dmi(path_str: str, ov_root: Path | None, cache: dict):
    if path_str not in cache:
        cache[path_str] = load_dmi(_resolve_source(path_str, ov_root))
    return cache[path_str]


def check_tile(manifest, rsi_dir: Path, diff_dir: Path, dmi_cache: dict, ov_root: Path | None):
    size_x = manifest["size"]["x"]
    size_y = manifest["size"]["y"]
    results = []

    for tile in manifest["tiles"]:
        png_path = rsi_dir / tile["png"]
        strip = Image.open(png_path).convert("RGBA")

        for variant in tile["variants"]:
            idx = variant["index"]
            # Extract generated cell from strip (horizontal layout).
            gen_cell = strip.crop((idx * size_x, 0, (idx + 1) * size_x, size_y))

            # Extract ground-truth cell from source DMI.
            _w, _h, states, cells_by_state = _cache_load_dmi(variant["source_dmi"], ov_root, dmi_cache)
            state_index = None
            for i, s in enumerate(states):
                if s["name"] == variant["source_state"]:
                    state_index = i
                    break
            if state_index is None:
                results.append({
                    "tile": tile["tile_id"],
                    "variant": idx,
                    "status": "SOURCE_STATE_MISSING",
                    "source_state": variant["source_state"],
                })
                continue
            src_cell = cells_by_state[state_index][0]  # dir0 frame0

            # Pixel diff.
            diff = ImageChops.difference(src_cell, gen_cell)
            bbox = diff.getbbox()
            if bbox is None:
                results.append({
                    "tile": tile["tile_id"],
                    "variant": idx,
                    "status": "MATCH",
                    "source_state": variant["source_state"],
                })
            else:
                # Count nonzero pixels and max per-channel delta.
                px = list(diff.getdata())
                nonzero = sum(1 for p in px if any(p[:4]))
                max_delta = max(max(p[:4]) for p in px)

                diff_dir.mkdir(parents=True, exist_ok=True)
                # Amplify diff for visibility.
                amp = Image.eval(diff, lambda v: min(255, v * 16))
                out = Image.new("RGBA", (size_x * 3 + 8, size_y + 20), (32, 32, 32, 255))
                out.paste(src_cell, (2, 18))
                out.paste(gen_cell, (size_x + 4, 18))
                out.paste(amp, (size_x * 2 + 6, 18))
                diag_name = f"{rsi_dir.stem}__{tile['tile_id']}__v{idx}.png"
                out.save(diff_dir / diag_name)

                results.append({
                    "tile": tile["tile_id"],
                    "variant": idx,
                    "status": "DIFFER",
                    "source_state": variant["source_state"],
                    "nonzero_px": nonzero,
                    "max_delta": max_delta,
                    "bbox": list(bbox),
                    "diag": str((diff_dir / diag_name).relative_to(diff_dir.parent)),
                })

    return results


def main():
    p = argparse.ArgumentParser()
    p.add_argument("rsi_out_root", help="Root containing generated *.rsi tile folders with _manifest.json")
    p.add_argument("--ov-root", default=None,
                   help="OV repo root, used to resolve relative source_dmi paths in the manifests. "
                        "Required when manifests were generated with --ov-root.")
    p.add_argument("--verbose", action="store_true")
    args = p.parse_args()

    root = Path(args.rsi_out_root)
    ov_root = Path(args.ov_root) if args.ov_root else None
    diff_dir = root / "_diff"
    # Clean stale diagnostics.
    if diff_dir.exists():
        for p_ in diff_dir.glob("*.png"):
            p_.unlink()

    dmi_cache: dict = {}
    total_variants = 0
    total_matches = 0
    total_differs = 0
    worst_delta = 0
    bad_tiles = []

    manifests = sorted(root.rglob("_manifest.json"))
    if not manifests:
        print(f"no _manifest.json files found under {root}", file=sys.stderr)
        return 2

    for mf in manifests:
        manifest = json.loads(mf.read_text(encoding="utf-8"))
        rsi_dir = mf.parent
        results = check_tile(manifest, rsi_dir, diff_dir, dmi_cache, ov_root)
        for r in results:
            total_variants += 1
            if r["status"] == "MATCH":
                total_matches += 1
                if args.verbose:
                    print(f"  ok  {rsi_dir.name}/{r['tile']}[{r['variant']}]")
            else:
                total_differs += 1
                worst_delta = max(worst_delta, r.get("max_delta", 0))
                bad_tiles.append((rsi_dir.name, r))
                print(f"  !!  {rsi_dir.name}/{r['tile']}[{r['variant']}] "
                      f"status={r['status']} "
                      f"nonzero={r.get('nonzero_px','?')} "
                      f"max_delta={r.get('max_delta','?')} "
                      f"src_state={r['source_state']}")

    print()
    print(f"Tile variants checked: {total_variants}")
    print(f"  match:  {total_matches}")
    print(f"  differ: {total_differs}  (worst max-channel-delta: {worst_delta})")
    if total_differs:
        print(f"Diff diagnostics: {diff_dir}")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
