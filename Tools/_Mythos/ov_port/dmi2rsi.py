"""
DMI -> RSI converter for porting Ochre-Valley (BYOND) sprites to Mythos (SS14).

DMI cell order within a state: frame-major (dir0f0, dir1f0, ..., dirNf0, dir0f1, ...)
RSI cell order within a state: direction-major (dir0f0, dir0f1, ..., dir1f0, ...)
-> converter transposes.

DMI direction storage order: SOUTH, NORTH, EAST, WEST, SE, SW, NE, NW
RSI direction order:         SOUTH, NORTH, EAST, WEST, SE, SW, NE, NW  (same)

Usage:
    python dmi2rsi.py <input.dmi> <output_dir.rsi> [--copyright "..."] [--license "..."]
    python dmi2rsi.py --batch <input_dir> <output_dir> [--copyright "..."] [--license "..."]
"""

import argparse
import json
import os
import re
import sys
from collections import defaultdict
from pathlib import Path

from PIL import Image


# License/copyright are NOT auto-populated; the source repo is not an accurate
# source for licensing info. Caller must pass --license/--copyright explicitly
# if they want those fields emitted; otherwise they are omitted from meta.json.


def parse_dmi_metadata(desc: str):
    """Parse the DMI Description text chunk. Returns (width, height, [state_dict...])."""
    width, height = 32, 32
    states = []
    current = None
    # Strip BOM and normalise
    desc = desc.lstrip("﻿")
    for raw in desc.splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        if "=" not in line:
            continue
        key, _, value = line.partition("=")
        key = key.strip()
        value = value.strip()
        if key == "version":
            continue
        if key == "width":
            width = int(value)
        elif key == "height":
            height = int(value)
        elif key == "state":
            # state = "name"
            name = value.strip().strip('"')
            current = {
                "name": name,
                "dirs": 1,
                "frames": 1,
                "delay": [],
                "loop": 0,
                "rewind": 0,
                "movement": 0,
                "hotspot": None,
            }
            states.append(current)
        elif current is not None:
            if key == "dirs":
                current["dirs"] = int(value)
            elif key == "frames":
                current["frames"] = int(value)
            elif key == "delay":
                current["delay"] = [float(x) for x in value.split(",") if x]
            elif key == "loop":
                current["loop"] = int(value)
            elif key == "rewind":
                current["rewind"] = int(value)
            elif key == "movement":
                current["movement"] = int(value)
            elif key == "hotspot":
                current["hotspot"] = [int(x) for x in value.split(",") if x]
    return width, height, states


def extract_description(img: Image.Image) -> str | None:
    # PIL decodes both tEXt and zTXt into img.info for known keys.
    desc = img.info.get("Description")
    if isinstance(desc, bytes):
        desc = desc.decode("utf-8", errors="replace")
    return desc


def safe_state_filename(name: str, used: set) -> str:
    # Preserve original name where filesystem-safe; replace problem chars.
    base = re.sub(r'[<>:"/\\|?*\x00-\x1f]', "_", name)
    if base == "":
        base = "_"
    candidate = base
    n = 1
    while candidate in used:
        candidate = f"{base}_{n}"
        n += 1
    used.add(candidate)
    return candidate


def load_dmi(dmi_path: Path):
    """Parse a DMI and return (width, height, states_meta, cells_by_state).

    cells_by_state is a list (parallel to states_meta) of lists of PIL.Image
    cells in DMI frame-major order (dir0f0, dir1f0, ..., dirNf0, dir0f1, ...).
    """
    img = Image.open(dmi_path)
    img.load()
    desc = extract_description(img)
    if desc is None:
        raise RuntimeError(f"{dmi_path}: no DMI Description metadata found")
    width, height, states = parse_dmi_metadata(desc)
    if not states:
        return width, height, [], []

    img = img.convert("RGBA")
    iw, _ih = img.size
    cols = iw // width
    if cols == 0:
        raise RuntimeError(f"{dmi_path}: image width {iw} < cell width {width}")

    cells_by_state = []
    cursor = 0
    for st in states:
        ncells = st["dirs"] * st["frames"]
        cells = []
        for i in range(ncells):
            idx = cursor + i
            cx = (idx % cols) * width
            cy = (idx // cols) * height
            cells.append(img.crop((cx, cy, cx + width, cy + height)))
        cells_by_state.append(cells)
        cursor += ncells
    return width, height, states, cells_by_state


def convert_dmi(dmi_path: Path, out_rsi: Path, copyright_: str | None, license_: str | None) -> int:
    width, height, states, cells_by_state = load_dmi(dmi_path)
    if not states:
        return 0

    out_rsi.mkdir(parents=True, exist_ok=True)
    meta_states = []
    used_names = set()

    for st, cells in zip(states, cells_by_state):
        ndirs = st["dirs"]
        nframes = st["frames"]
        ncells = ndirs * nframes

        # Transpose to direction-major: out[d*nframes + f] = dmi[f*ndirs + d]
        rsi_cells = [None] * ncells
        for f in range(nframes):
            for d in range(ndirs):
                dmi_idx = f * ndirs + d
                rsi_idx = d * nframes + f
                rsi_cells[rsi_idx] = cells[dmi_idx]

        # Pack into a near-square grid (reading order).
        import math
        dim_x = max(1, math.ceil(math.sqrt(ncells)))
        dim_y = math.ceil(ncells / dim_x)
        sheet = Image.new("RGBA", (dim_x * width, dim_y * height), (0, 0, 0, 0))
        for i, c in enumerate(rsi_cells):
            sx = (i % dim_x) * width
            sy = (i // dim_x) * height
            sheet.paste(c, (sx, sy))

        # State name must match the PNG filename (RSI loader resolves "<name>.png").
        fname = safe_state_filename(st["name"], used_names)
        sheet.save(out_rsi / f"{fname}.png")

        state_entry = {"name": fname}
        if ndirs in (4, 8):
            state_entry["directions"] = ndirs
        # Only include delays if animated (frames > 1) and we have delays.
        if nframes > 1 and st["delay"]:
            # RSI delays: array per direction. DMI has one delay list shared across dirs.
            delay_list = st["delay"][:nframes]
            if len(delay_list) < nframes:
                delay_list = delay_list + [delay_list[-1]] * (nframes - len(delay_list))
            # DMI delays are in 1/10 second units; RSI delays are seconds.
            delay_list = [round(d / 10.0, 3) for d in delay_list]
            state_entry["delays"] = [list(delay_list) for _ in range(max(1, ndirs))]
        meta_states.append(state_entry)

    meta = {"version": 1}
    if license_:
        meta["license"] = license_
    if copyright_:
        meta["copyright"] = copyright_
    meta["size"] = {"x": width, "y": height}
    meta["states"] = meta_states
    with open(out_rsi / "meta.json", "w", encoding="utf-8") as f:
        json.dump(meta, f, indent=2)
        f.write("\n")

    return len(meta_states)


def batch_convert(in_dir: Path, out_dir: Path, copyright_: str | None, license_: str | None):
    dmi_files = sorted(in_dir.rglob("*.dmi"))
    total_states = 0
    ok = 0
    fail = 0
    for dmi in dmi_files:
        rel = dmi.relative_to(in_dir).with_suffix("")
        # Mirror directory structure, turning DMI basename into an .rsi folder.
        out_rsi = out_dir / rel.parent / f"{rel.name}.rsi"
        try:
            n = convert_dmi(dmi, out_rsi, copyright_, license_)
            if n == 0:
                print(f"[skip] {dmi.relative_to(in_dir)} (no states)")
                continue
            total_states += n
            ok += 1
            print(f"[ok]   {dmi.relative_to(in_dir)} -> {out_rsi.relative_to(out_dir)} ({n} states)")
        except Exception as e:
            fail += 1
            print(f"[fail] {dmi.relative_to(in_dir)}: {e}", file=sys.stderr)
    print(f"\nConverted {ok} DMI -> RSI ({total_states} states total), {fail} failed.")


def main():
    p = argparse.ArgumentParser()
    p.add_argument("input")
    p.add_argument("output")
    p.add_argument("--batch", action="store_true")
    p.add_argument("--copyright", default=None,
                   help="Optional. If omitted, no copyright field is emitted to meta.json.")
    p.add_argument("--license", default=None,
                   help="Optional. If omitted, no license field is emitted to meta.json.")
    args = p.parse_args()

    inp = Path(args.input)
    out = Path(args.output)
    if args.batch:
        batch_convert(inp, out, args.copyright, args.license)
    else:
        n = convert_dmi(inp, out, args.copyright, args.license)
        print(f"{inp.name} -> {out} ({n} states)")


if __name__ == "__main__":
    main()
