from pathlib import Path
import hashlib,json
root=Path(__file__).resolve().parents[3];r=Path(__file__).resolve().parent
allowed={'Assets/Scripts/UI/BoardTileOverlayWidget.cs','Assets/Scripts/Board/BoardVisualTheme.cs','Assets/Scripts/Visuals/BoardSurfaceLighting.cs','Assets/Scripts/Visuals/BoardContactFeedback.cs','Assets/Scripts/Visuals/CompactPawnVisual.cs','Assets/Shaders/GraphiteSurface.shader','Assets/Shaders/NeonState.shader','Assets/Settings/GraphiteFoundation/GraphiteTheme.asset'}
changed=[];missing=[]
for row in json.loads((r/'baseline-manifest.json').read_text()):
 p=root/row['path']
 if not p.is_file():missing.append(row['path'])
 elif hashlib.sha256(p.read_bytes()).hexdigest()!=row['sha256']:changed.append(row['path'])
protected=[row for row in json.loads((r/'baseline-manifest.json').read_text()) if row['path'].startswith(('Assets/Scripts/Core/','Assets/Scripts/Multiplayer/')) or row['path'] in {'Assets/Scripts/Systems/BoardInteractionManager.cs','Assets/Scripts/Visuals/PawnMovement.cs','Assets/Scripts/Board/PolyRiskBoard.cs'} or row['path'].startswith('Server/')]
result={'baselineFiles':639,'changed':changed,'missing':missing,'unexpected':[p for p in changed if p not in allowed],'protectedFiles':len(protected),'protectedChanged':[p['path'] for p in protected if p['path'] in changed or p['path'] in missing]}
result['pass']=not result['unexpected'] and not missing and not result['protectedChanged']
(r/'scope-validation.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2));assert result['pass']
