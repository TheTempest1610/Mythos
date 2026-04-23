"""
Strip named components from prototype YAML files.

A "component" is a YAML map entry appearing in a `components:` list with:
    - type: ComponentName
      key1: value1
      key2: value2

This strips the `- type: Foo` line and every subsequent more-indented line
(the component's children) until we hit a sibling or less-indented line.
Preserves YAML structure; doesn't touch other components.
"""
import sys, re, argparse

def strip_components(text, targets):
    """Return text with all top-level occurrences of `- type: X\n   ...children` removed, where X in targets."""
    lines = text.splitlines(keepends=True)
    out = []
    i = 0
    while i < len(lines):
        line = lines[i]
        # Match "  - type: Foo" with optional trailing whitespace/comment
        m = re.match(r'^(\s*)-\s*type:\s*(\w+)\s*(#.*)?$', line)
        if m and m.group(2) in targets:
            base_indent = len(m.group(1))
            # Skip this line and continuation (anything at indent > base_indent that isn't another - item)
            i += 1
            while i < len(lines):
                nl = lines[i]
                stripped = nl.lstrip(' ')
                if stripped == '' or stripped == '\n' or stripped.startswith('#'):
                    # comment/blank line - consume if still in block
                    nl_indent = len(nl) - len(stripped)
                    if nl_indent > base_indent or stripped == '\n' or stripped == '':
                        i += 1
                        continue
                    else:
                        break
                nl_indent = len(nl) - len(stripped)
                # Child of the component (deeper indent): skip
                if nl_indent > base_indent:
                    i += 1
                    continue
                # Same or shallower indent = end of this component block
                break
            continue
        out.append(line)
        i += 1
    return ''.join(out)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--components', required=True, help='Comma-separated list of component names to strip')
    ap.add_argument('files', nargs='+')
    args = ap.parse_args()
    targets = set(args.components.split(','))
    changed = 0
    for path in args.files:
        with open(path, encoding='utf-8', errors='replace') as f:
            txt = f.read()
        new_txt = strip_components(txt, targets)
        if new_txt != txt:
            with open(path, 'w', encoding='utf-8', newline='\n') as f:
                f.write(new_txt)
            changed += 1
            print(f"modified: {path}")
    print(f"done: {changed} files changed")

if __name__ == '__main__':
    main()
