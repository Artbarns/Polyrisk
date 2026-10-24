"""Compare PASS 2A.1 to the pre-iteration working tree, preserving unrelated local work."""
import hashlib
import json
from pathlib import Path

project = Path(__file__).resolve().parents[3]
report = Path(__file__).resolve().parent
before = json.loads((report / 'before-inventory.json').read_text())
allowed = {
    'Assets/Scripts/Board/PolyRiskBoard.cs',
    'Assets/Scripts/Board/BoardVisualTheme.cs',
    'Assets/Shaders/GraphiteSurface.shader',
    'Assets/Settings/GraphiteFoundation/GraphiteTheme.asset',
    'Assets/Settings/GraphiteFoundation/GraphiteFrame.mat',
    'Assets/Settings/GraphiteFoundation/GraphiteTiles.mat',
    'Assets/Settings/GraphiteFoundation/GraphiteEngraving.mat',
    'Assets/Settings/GraphiteFoundation/GraphiteAccents.mat',
}
changed = [name for name, value in before.items() if not (project / name).exists()
           or hashlib.sha256((project / name).read_bytes()).hexdigest() != value]
unexpected = sorted(set(changed) - allowed)
assert not unexpected, unexpected
assert 'Assets/Scenes/SampleScene.unity' not in changed
board = (project / 'Assets/Scripts/Board/PolyRiskBoard.cs').read_text()
original = (report / 'baseline/Assets/Scripts/Board/PolyRiskBoard.cs').read_text()
for start, end in [('        void BuildSlotModel()', '        void AssertSlot'),
                   ('        void Generate(bool preserveAnchors)', '        void ApplyMaterialSettings()')]:
    assert board[board.index(start):board.index(end)] == original[original.index(start):original.index(end)]
new_assets = sorted(str(f.relative_to(project)) for root in ['Assets', 'ProjectSettings']
                    for f in (project / root).rglob('*') if f.is_file() and str(f.relative_to(project)) not in before)
expected_new = sorted(['Assets/Scripts/Debug/GraphiteVisualIsolation.cs',
                       'Assets/Scripts/Debug/GraphiteVisualIsolation.cs.meta',
                       'Assets/Scripts/Editor/GraphiteRefinementValidation.cs',
                       'Assets/Scripts/Editor/GraphiteRefinementValidation.cs.meta'])
assert new_assets == expected_new, new_assets
result = {'changed_existing_files': sorted(changed), 'created_assets': new_assets, 'unexpected_changes': unexpected,
          'preexisting_files_unchanged': len(before) - len(changed),
          'scene_and_embedded_meshes_byte_identical': True,
          'geometry_and_anchor_generation_code_unchanged': True}
log_path = report / 'build.log'
if log_path.exists():
    log = log_path.read_text()
    assert 'WEBGL PLAYER BUILD PASS' in log
    assert 'Shader error' not in log and 'Shader warning' not in log and 'error CS' not in log
    result['final_build_no_shader_errors_or_warnings'] = True
(report / 'scope-validation.json').write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps(result, indent=2))
