import json,hashlib,difflib
from pathlib import Path
r=Path(__file__).resolve().parent;root=r.parents[2]
before=json.loads((r/'before-inventory.json').read_text())
after={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for d in ['Assets','ProjectSettings'] for p in (root/d).rglob('*') if p.is_file()}
changed=sorted(p for p in before if after.get(p)!=before[p]);created=sorted(set(after)-set(before))
expected=sorted(['Assets/Scripts/Visuals/CompactPawnVisual.cs','Assets/Scripts/Visuals/PawnMovement.cs','Assets/Shaders/GraphiteSurface.shader','Assets/Shaders/NeonState.shader','Assets/Scripts/Debug/MonolithValidationDriver.cs','Assets/Scripts/Editor/MonolithValidation.cs'])
assert changed==expected,changed
assert created==['Assets/Scripts/Visuals/BoardContactFeedback.cs','Assets/Scripts/Visuals/BoardContactFeedback.cs.meta'],created
f='Assets/Scripts/Visuals/PawnMovement.cs';new=(root/f).read_text();old=(r/'baseline'/f).read_text()
stripped=new.replace('        // Read-only presentation clock; the original completion callback keeps its timing.\n        public bool IsVisuallySettled { get; private set; } = true;\n\n','').replace('            IsVisuallySettled = false;\n','').replace('            IsVisuallySettled = true;\n','')
assert stripped==old,'Movement algorithms or callback changed'
out={'modified':changed,'created':created,'unchangedExistingAssetsSettings':sum(after.get(p)==v for p,v in before.items()),'movementAlgorithmAndCallback':'identical after removing read-only presentation clock','cameraPASS2D':'byte-identical','geometry40IndicesCollidersSelectionRulesEconomyNetworkDiceCockpitScenePrefab':'byte-identical'}
(r/'scope-validation.json').write_text(json.dumps(out,indent=2));(r/'after-inventory.json').write_text(json.dumps(after,indent=2))
diff=[]
for f in changed:diff.extend(difflib.unified_diff((r/'baseline'/f).read_text().splitlines(True),(root/f).read_text().splitlines(True),fromfile='before/'+f,tofile='after/'+f))
for f in created:diff.extend(difflib.unified_diff([],(root/f).read_text().splitlines(True),fromfile='/dev/null',tofile='after/'+f))
(r/'pass-2e1-only.diff').write_text(''.join(diff));print(json.dumps(out,indent=2))
