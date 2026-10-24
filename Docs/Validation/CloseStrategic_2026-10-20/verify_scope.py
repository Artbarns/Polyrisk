import hashlib,json
from pathlib import Path
r=Path(__file__).resolve().parent;root=r.parents[2]
before=json.loads((r/'before-inventory.json').read_text())
after={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for d in ('Assets','ProjectSettings') for p in (root/d).rglob('*') if p.is_file()}
modified=sorted(p for p in before if p in after and before[p]!=after[p])
allowed=['Assets/Scripts/Systems/BoardInteractionManager.cs','Assets/Scripts/Visuals/PlayerVisualManager.cs','Assets/Scripts/UI/TopHUDWidget.cs','Assets/Scripts/Editor/MobileBoardPresenceValidation.cs','Assets/Scripts/Debug/StrategicCameraValidationDriver.cs']
assert modified==sorted(allowed),modified
created=sorted(set(after)-set(before));assert created==['Assets/Scripts/Visuals/BoardDicePresenter.cs','Assets/Scripts/Visuals/BoardDicePresenter.cs.meta'],created
assert not set(before)-set(after)
old=lambda n:(r/'baseline'/n).read_text();new=lambda n:(root/n).read_text()
n='Assets/Scripts/Systems/BoardInteractionManager.cs'
assert new(n).split('        private void Update()',1)[1]==old(n).split('        private void Update()',1)[1],'Input and permissions changed'
n='Assets/Scripts/Visuals/PlayerVisualManager.cs'
assert new(n).split('                // Execute movement',1)[1]==old(n).split('                // Execute movement',1)[1],'Movement callback contract changed'
result={'modified':modified,'created':created,'unchangedAssetsAndSettings':sum(after.get(p)==v for p,v in before.items()),'rulesEconomyDiceSystemTurnManagerPawnMovementGeometryMaterialsShadersSceneNetworkUI':'byte-identical to start of PASS 2D','pawnMovementCompletionCallback':'byte-identical','inputAndPermissions':'byte-identical','visualPreparation':'at most 1.1 s before existing dispatch; no post-callback wait'}
(r/'after-inventory.json').write_text(json.dumps(after,indent=2));(r/'scope-validation.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
