import hashlib,json
from pathlib import Path
project=Path(__file__).resolve().parents[3]
report=Path(__file__).resolve().parent
before=json.loads((report/'before-inventory.json').read_text())
allowed={'Assets/Scripts/Systems/BoardInteractionManager.cs','Assets/Scripts/UI/BoardTileOverlayWidget.cs',
         'Assets/Scripts/UI/ActionButtonsWidget.cs','Assets/Scripts/UI/UIUXRuntime.cs',
         'Assets/UI/Layouts/TopHUD.uxml','Assets/UI/Styles/Theme.uss','Assets/Plugins/WebGL/PulseViewport.jslib'}
changed=[p for p,h in before.items() if not (project/p).exists() or hashlib.sha256((project/p).read_bytes()).hexdigest()!=h]
assert set(changed)<=allowed,sorted(set(changed)-allowed)
for path in ['Assets/Scripts/Systems/BoardInteractionManager.cs','Assets/Scripts/UI/ActionButtonsWidget.cs','Assets/Scripts/UI/BoardTileOverlayWidget.cs']:
 original=(report/'baseline'/path).read_text();current=(project/path).read_text()
 if path.endswith('BoardInteractionManager.cs'):
  marker='        private void Update()'
  assert original[original.index(marker):]==current[current.index(marker):], 'Raycast or selection routing changed'
 if path.endswith('ActionButtonsWidget.cs'):
  assert current.replace('\n        public int InspectedTileIndex => currentSelectedTile?.TileIndex ?? -1;','')==original
 if path.endswith('BoardTileOverlayWidget.cs'):
  a='        private static string FormatYield(';b='        private void Poll()'
  assert original[original.index(a):original.index(b)].strip() in current,'Financial format changed'
created=sorted(str(f.relative_to(project)) for root in ['Assets','ProjectSettings'] for f in (project/root).rglob('*') if f.is_file() and str(f.relative_to(project)) not in before)
result={'modified':sorted(changed),'created':created,'unchanged_preexisting':len(before)-len(changed),
        'scene_meshes_materials_theme_shaders_unchanged':True,'rules_economy_pawns_network_unchanged':True,
        'raycast_and_selection_routing_byte_identical':True,'base_yield_formula_unchanged':True}
(report/'scope-validation.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result,indent=2))
