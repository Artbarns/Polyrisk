"""PASS 2B.2 scope check against the dirty workspace preserved at start."""
import hashlib,json
from pathlib import Path
report=Path(__file__).resolve().parent
root=report.parents[2]
before=json.loads((report/'before-inventory.json').read_text())
after={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for folder in ('Assets','ProjectSettings') for p in (root/folder).rglob('*') if p.is_file()}
changed=sorted(p for p in before if p in after and before[p]!=after[p])
allowed=['Assets/Scripts/Systems/BoardInteractionManager.cs','Assets/UI/Layouts/TopHUD.uxml','Assets/UI/Styles/Theme.uss','Assets/Scripts/Editor/MobileBoardPresenceValidation.cs','Assets/Scripts/Debug/MobileBoardValidationProbe.cs']
assert changed==sorted(allowed),changed
assert set(after)==set(before), 'Unexpected runtime assets added/deleted'
name='Assets/Scripts/Systems/BoardInteractionManager.cs'
old=(report/'baseline'/name).read_text().split('        private void Update()',1)[1]
new=(root/name).read_text().split('        private void Update()',1)[1]
a=new.index('            // Presentation only:');b=new.index('            Debug.Log("[Input Gateway] Routed click to GameEvents.");',a)
assert new[:a]+new[b:]==old,'Input/raycast/permission/selection routes changed beyond presentation bookkeeping'
name='Assets/UI/Layouts/TopHUD.uxml'
old=(report/'baseline'/name).read_text()
assert (root/name).read_text().replace('            <ui:Button name="Btn_BoardFocus" text="Approcher" class="board-camera-control" />\n','')==old
name='Assets/UI/Styles/Theme.uss'
assert (root/name).read_text().startswith((report/'baseline'/name).read_text())
result={'modified':changed,'newRuntimeAssets':[],'deletedRuntimeAssets':[],'unchangedAssetsAndSettings':sum(after[p]==h for p,h in before.items()),'rulesEconomyInspectorPawnsMovementCallbacksNetworkSceneGeometryShadersMaterials':'byte-identical','inputAndPermissionRouting':'unchanged; only remembers a successful manual inspection for the camera'}
(report/'after-inventory.json').write_text(json.dumps(after,indent=2))
(report/'scope-validation.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result,indent=2))
