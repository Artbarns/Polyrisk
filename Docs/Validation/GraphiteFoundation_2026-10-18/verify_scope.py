"""Compare against the working tree captured before PASS 2A, never against Git HEAD."""
import hashlib
import json
import re
from pathlib import Path

project = Path(__file__).resolve().parents[3]
report = Path(__file__).resolve().parent
before = json.loads((report / 'before-inventory.json').read_text())
changed = []
for name, digest in before.items():
    path = project / name
    current = hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else 'MISSING'
    if current != digest:
        changed.append(name)
allowed = {'Assets/Scripts/Board/PolyRiskBoard.cs', 'Assets/Scenes/SampleScene.unity'}
unexpected = [name for name in changed if name not in allowed]
board = (project / 'Assets/Scripts/Board/PolyRiskBoard.cs').read_text()
original_board = (report / 'baseline/Assets/Scripts/Board/PolyRiskBoard.cs').read_text()
for start, end in [('        void BuildSlotModel()', '        void AssertSlot'),
                   ('        void BuildSlotAnchors()', '        GameObject FindOrCreateChild')]:
    assert board[board.index(start):board.index(end)] == original_board[original_board.index(start):original_board.index(end)], start
def blocks(text):
    chunks = re.split(r'(?=^--- !u!)', text, flags=re.M)
    return {re.match(r'--- !u!\d+ &(-?\d+)', c)[1]: c.strip() for c in chunks if re.match(r'--- !u!\d+ &(-?\d+)', c)}
original_scene = blocks((report / 'baseline/Assets/Scenes/SampleScene.unity').read_text())
current_scene = blocks((project / 'Assets/Scenes/SampleScene.unity').read_text())
anchor_objects = 0
for identity, block in original_scene.items():
    if not re.search(r'^  m_Name: Tile_\d+$', block, re.M):
        continue
    anchor_objects += 1
    assert current_scene[identity] == block, 'Anchor object changed: ' + identity
    for component in re.findall(r'- component: \{fileID: (-?\d+)\}', block):
        assert current_scene[component] == original_scene[component], 'Anchor component changed: ' + component
assert anchor_objects == 40
final_build = (report / 'foundation-build.log').read_text()
assert 'WEBGL PLAYER BUILD PASS' in final_build
assert 'Shader error' not in final_build
assert 'error CS' not in final_build
result = {'serialized_anchor_objects_and_components_unchanged': anchor_objects,
          'final_webgl_build_without_shader_or_csharp_errors': True,
          'index_and_collider_creation_methods_unchanged': True, 'changed_existing_files': changed, 'unexpected_changes': unexpected,
          'protected_existing_files_unchanged': len(before) - len(changed)}
(report / 'scope-validation.json').write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps(result, indent=2))
if unexpected:
    raise SystemExit('Unexpected existing-file changes; inspect before delivery.')
