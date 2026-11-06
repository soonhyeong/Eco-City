"""Optional C# syntax check: pip install tree-sitter tree-sitter-c-sharp.

This is not a replacement for Unity compilation or the executable Core checks.
"""
from pathlib import Path
import sys

from tree_sitter import Language, Parser
import tree_sitter_c_sharp

root = Path(__file__).resolve().parents[1]
parser = Parser(Language(tree_sitter_c_sharp.language()))
files = sorted(root.rglob("*.cs"))
errors = []
for path in files:
    tree = parser.parse(path.read_bytes())
    if tree.root_node.has_error:
        nodes = [tree.root_node]
        while nodes:
            node = nodes.pop()
            if node.type == "ERROR" or node.is_missing:
                errors.append(f"{path.relative_to(root)}:{node.start_point.row + 1}: {node.type}")
            nodes.extend(node.children)
for error in errors:
    print(error)
print(f"C# files: {len(files)}; syntax errors: {len(errors)}")
sys.exit(bool(errors))
