import json,hashlib,difflib
from pathlib import Path
r=Path(__file__).resolve().parent;root=r.parents[2]
before=json.loads((r/'before-inventory.json').read_text())
after={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for d in ['Assets','ProjectSettings'] for p in (root/d).rglob('*') if p.is_file()}
changed=[p for p in before if after.get(p)!=before[p]]
assert changed==['Assets/Scripts/Visuals/PlayerVisualManager.cs'],changed
created=sorted(set(after)-set(before));assert len(created)==6,created
for f in ['Assets/Scripts/Visuals/PawnMovement.cs','Assets/Scripts/Systems/BoardInteractionManager.cs']:
 assert (r/'baseline'/f).read_bytes()==(root/f).read_bytes()
f='Assets/Scripts/Visuals/PlayerVisualManager.cs'
old=(r/'baseline'/f).read_text();new=(root/f).read_text()
assert old.split('        private void HandlePlayerPath',1)[1]==new.split('        private void HandlePlayerPath',1)[1], 'dispatch or completion callback changed'
out={'modified':changed,'created':created,'unchangedExistingAssetsSettings':sum(after.get(p)==v for p,v in before.items()),'PawnMovement':'byte-identical','CameraPASS2D':'byte-identical','pathDicePreparationAndCallback':'byte-identical','economyRulesTurnManagerNetworkScenePrefabBoardShadersUI':'byte-identical'}
(r/'scope-validation.json').write_text(json.dumps(out,indent=2));(r/'after-inventory.json').write_text(json.dumps(after,indent=2))
diff=list(difflib.unified_diff(old.splitlines(True),new.splitlines(True),fromfile='before/'+f,tofile='after/'+f))
for n in created:diff.extend(difflib.unified_diff([], (root/n).read_text().splitlines(True),fromfile='/dev/null',tofile='after/'+n))
(r/'pass-2e-only.diff').write_text(''.join(diff));print(json.dumps(out,indent=2))
