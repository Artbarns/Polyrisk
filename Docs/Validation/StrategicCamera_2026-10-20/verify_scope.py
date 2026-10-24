import hashlib,json
from pathlib import Path
r=Path(__file__).resolve().parent;root=r.parents[2]
before=json.loads((r/'before-inventory.json').read_text())
after={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for d in ('Assets','ProjectSettings') for p in (root/d).rglob('*') if p.is_file()}
modified=sorted(p for p in before if p in after and before[p]!=after[p])
allowed=['Assets/Scripts/Systems/BoardInteractionManager.cs','Assets/Scripts/Visuals/PlayerVisualManager.cs','Assets/Scripts/UI/ActionButtonsWidget.cs','Assets/Scripts/Multiplayer/OnlineGameController.cs','Assets/UI/Layouts/TopHUD.uxml','Assets/UI/Styles/Theme.uss','Assets/Scripts/Editor/MobileBoardPresenceValidation.cs','Assets/Scripts/Debug/MobileBoardValidationProbe.cs']
assert modified==sorted(allowed),modified
created=sorted(set(after)-set(before));assert created==['Assets/Scripts/Debug/StrategicCameraValidationDriver.cs','Assets/Scripts/Debug/StrategicCameraValidationDriver.cs.meta'],created
assert not set(before)-set(after)
old=lambda n:(r/'baseline'/n).read_text();new=lambda n:(root/n).read_text()
n='Assets/Scripts/Systems/BoardInteractionManager.cs';expected=old(n).split('        private void Update()',1)[1];a=expected.index('            // Presentation only:');b=expected.index('            Debug.Log("[Input Gateway] Routed click to GameEvents.");',a)
assert new(n).split('        private void Update()',1)[1]==expected[:a]+expected[b:],'Input/permissions changed'
n='Assets/Scripts/Visuals/PlayerVisualManager.cs';assert new(n).split('                // Execute movement',1)[1]==old(n).split('                // Execute movement',1)[1],'Movement callback contract changed'
n='Assets/Scripts/Multiplayer/OnlineGameController.cs';expected=old(n).replace('        public event Action GameReady;','        public event Action GameReady;\n        public static event Action PresentationStateApplied;').replace('            return turns.ApplyReplicatedOutcome(snapshot.isGameOver,\n                snapshot.winnerPlayerId, snapshot.victoryReason);','            bool applied = turns.ApplyReplicatedOutcome(snapshot.isGameOver,\n                snapshot.winnerPlayerId, snapshot.victoryReason);\n            if (applied) PresentationStateApplied?.Invoke();\n            return applied;')
assert new(n)==expected,'Network logic changed beyond local presentation signal'
result={'modified':modified,'created':created,'unchangedAssetsAndSettings':sum(after.get(p)==v for p,v in before.items()),'rulesEconomyTurnManagerPawnMovementGeometryMaterialsShadersSceneNetworkDtos':'byte-identical','pawnMovementCompletionCallback':'byte-identical','networkApplyState':'same result/logic plus local presentation-success notification','inputAndPermissions':'unchanged; obsolete manual focus bookkeeping removed'}
(r/'after-inventory.json').write_text(json.dumps(after,indent=2));(r/'scope-validation.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
