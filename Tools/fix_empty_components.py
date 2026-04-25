"""
Undo `components: []` lines that were incorrectly inserted before list items.

A previous run of this script (with a buggy lookahead) replaced `components:` with
`components: []` even when the next line was a `- type:` list item at the SAME indent
as the key — which IS valid YAML for an inline list. The `[]` then made YAML treat
the list as empty, breaking inheritance.

This pass:
- Looks at each `components: []` line.
- If the next non-blank/non-comment line is a `- type:` at indent >= the components
  line's indent, remove the `[]` (turning it back into `components:`).
- Otherwise the `[]` is legitimately marking an empty list — leave it.
"""
import sys, re

def fix(text):
    lines = text.splitlines(keepends=True)
    out = []
    i = 0
    changed = 0
    while i < len(lines):
        line = lines[i]
        m = re.match(r'^(\s*)components:\s*\[\]\s*$', line)
        if m:
            indent = len(m.group(1))
            # Look ahead for a `- ` list item at >= indent
            j = i + 1
            has_items = False
            while j < len(lines):
                nl = lines[j]
                stripped = nl.lstrip(' ')
                if stripped == '' or stripped == '\n' or stripped.startswith('#'):
                    j += 1
                    continue
                nl_indent = len(nl) - len(stripped)
                if nl_indent >= indent and stripped.startswith('- '):
                    has_items = True
                break
            if has_items:
                out.append(' ' * indent + 'components:\n')
                changed += 1
                i += 1
                continue
        out.append(line)
        i += 1
    return ''.join(out), changed

def main():
    total = 0
    files_changed = 0
    for path in sys.argv[1:]:
        with open(path, encoding='utf-8', errors='replace') as f:
            txt = f.read()
        new_txt, n = fix(txt)
        if n > 0:
            with open(path, 'w', encoding='utf-8', newline='\n') as f:
                f.write(new_txt)
            files_changed += 1
            total += n
            print(f"modified: {path} ({n} block(s))")
    print(f"done: {files_changed} files changed, {total} blocks fixed")

if __name__ == '__main__':
    main()
