"""Verify PASS 2B.1 against the actual dirty workspace at its start, never against HEAD."""
import hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[3]
report=Path(__file__).resolve().parent
before=json.loads((report/'before-inventory.json').read_text())
after={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for folder in ('Assets','ProjectSettings') for p in (root/folder).rglob('*') if p.is_file()}
changed=sorted(p for p in before if p in after and before[p]!=after[p])
deleted=sorted(set(before)-set(after)); created=sorted(set(after)-set(before))
allowed=['Assets/Scripts/UI/BoardTileOverlayWidget.cs','Assets/Scripts/UI/TopHUDWidget.cs','Assets/Scripts/UI/Marketplace_UIManager.cs','Assets/Scripts/Systems/BoardInteractionManager.cs','Assets/UI/Layouts/TopHUD.uxml','Assets/UI/Styles/Theme.uss','Assets/Scripts/Debug/MobileBoardValidationProbe.cs','Assets/Scripts/Editor/MobileBoardPresenceValidation.cs']
assert changed==sorted(allowed),(changed,allowed)
assert deleted==['Assets/Scripts/Debug/GraphiteVisualIsolation.cs','Assets/Scripts/Debug/GraphiteVisualIsolation.cs.meta'],deleted
assert not created,created
read=lambda name:(root/name).read_text()
old=lambda name:(report/'baseline'/name).read_text()
path='Assets/Scripts/Systems/BoardInteractionManager.cs'
assert old(path).split('        private void Update()',1)[1]==read(path).split('        private void Update()',1)[1], 'Input/raycasts/permissions changed'
for path in ['Assets/Scripts/UI/TopHUDWidget.cs','Assets/Scripts/UI/Marketplace_UIManager.cs']:
 expected=old(path).replace('SetBoardYieldOverlayVisible','SetBoardIdentityOverlayVisible').replace('BoardTileYieldOverlay','BoardTileIdentityOverlay').replace('RestoreYieldOverlayAfterDice','RestoreBoardIdentityAfterDice')
 assert read(path)==expected, 'Only identity overlay naming may change in '+path
assert after['Assets/Scripts/UI/ActionButtonsWidget.cs']==before['Assets/Scripts/UI/ActionButtonsWidget.cs']
result={'modified':changed,'deleted':deleted,'createdAssets':created,'unchangedFiles':sum(after.get(p)==h for p,h in before.items()),'inputAndPermissionRouting':'byte-identical','inspectorAndEconomicSources':'byte-identical','sceneGeometryShadersMaterialsPawnsNetwork':'byte-identical'}
(report/'after-inventory.json').write_text(json.dumps(after,indent=2))
(report/'scope-validation.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result,indent=2))
