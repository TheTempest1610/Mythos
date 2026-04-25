"""
Seed missing locale keys for tile prototypes.

Scans tile prototype YAMLs under <proto_dir>, collects every `name:` field,
checks against existing keys defined anywhere under Locale/en-US/, and appends
placeholder entries for the missing ones to <ftl_path>.

Placeholder English string: derived from the key's tail segment with
underscores/hyphens converted to spaces. Easy to bulk-replace later with
proper translations.

Usage:
    python seed_locale.py <proto_dir> <ftl_path> [--repo PATH]
"""
import argparse
import re
import sys
from pathlib import Path

import yaml


def collect_locale_keys(locale_root: Path) -> set:
    keys = set()
    key_re = re.compile(r"^([a-z][a-z0-9_-]*)\s*=", re.MULTILINE)
    for ftl in locale_root.rglob("*.ftl"):
        for m in key_re.finditer(ftl.read_text(encoding="utf-8", errors="replace")):
            keys.add(m.group(1))
    return keys


def collect_tile_locale_keys(proto_dir: Path) -> dict:
    """Return {locale_key: tile_id} for every tile under proto_dir."""
    out = {}
    for yml in proto_dir.rglob("*.yml"):
        try:
            for doc in yaml.safe_load_all(yml.read_text(encoding="utf-8")):
                if not isinstance(doc, list):
                    continue
                for entry in doc:
                    if not isinstance(entry, dict) or entry.get("type") != "tile":
                        continue
                    key = entry.get("name")
                    if key:
                        out.setdefault(key, entry.get("id", "?"))
        except yaml.YAMLError as e:
            print(f"[warn] {yml}: {e}", file=sys.stderr)
    return out


def humanize(key: str) -> str:
    """tiles-ov-snow-snow_surround -> 'snow surround'.

    Strip the conventional `tiles-<namespace>-<category>-` prefix; everything
    after is the per-tile name. Replace _ and - with spaces.
    """
    parts = key.split("-", 3)
    name_part = parts[3] if len(parts) >= 4 else parts[-1]
    return name_part.replace("_", " ").replace("-", " ")


def main():
    p = argparse.ArgumentParser()
    p.add_argument("proto_dir", help="Directory of tile prototype YAMLs to scan.")
    p.add_argument("ftl_path", help="Target .ftl file to append seeds to (created if absent).")
    p.add_argument("--repo", default=r"c:/repos/Mythos", help="Mythos repo root")
    args = p.parse_args()

    repo = Path(args.repo)
    proto_dir = Path(args.proto_dir)
    ftl_path = Path(args.ftl_path)
    locale_root = repo / "Resources" / "Locale" / "en-US"

    existing = collect_locale_keys(locale_root)
    needed = collect_tile_locale_keys(proto_dir)

    missing = sorted(k for k in needed if k not in existing)
    if not missing:
        print("All tile locale keys already defined. Nothing to seed.")
        return 0

    ftl_path.parent.mkdir(parents=True, exist_ok=True)
    appending = ftl_path.exists()
    with open(ftl_path, "a" if appending else "w", encoding="utf-8") as f:
        if not appending:
            f.write("# Auto-seeded by Tools/_Mythos/ov_port/seed_locale.py\n")
            f.write("# Replace with proper English strings as time permits.\n\n")
        else:
            f.write("\n# --- seeded by seed_locale.py ---\n")
        for key in missing:
            f.write(f"{key} = {humanize(key)}\n")

    print(f"Seeded {len(missing)} missing keys into {ftl_path}.")
    print(f"({len(needed) - len(missing)} were already present, {len(needed)} total tile keys scanned.)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
