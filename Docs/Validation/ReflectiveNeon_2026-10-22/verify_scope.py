import json,hashlib,difflib
from pathlib import Path
r=Path(__file__).resolve().parent;root=r.parents[2]
before=json.loads((r/'before-inventory.json').read_text());after={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for d in ['Assets','ProjectSettings','Packages'] for p in (root/d).rglob('*') if p.is_file()}
changed=sorted(p for p in before if after.get(p)!=before[p]);created=sorted(set(after)-set(before))
expected=sorted(['Assets/Scripts/Board/BoardVisualTheme.cs','Assets/Scripts/Visuals/CompactPawnVisual.cs','Assets/Scripts/Visuals/BoardContactFeedback.cs','Assets/Shaders/GraphiteSurface.shader','Assets/Shaders/NeonState.shader','Assets/Settings/Low_PipelineAsset.asset','Assets/Settings/GraphiteFoundation/GraphiteTheme.asset'])
assert changed==expected,changed
assert len(created)==6,created
f='Assets/Scripts/Visuals/BoardContactFeedback.cs';old=(r/'baseline'/f).read_text();new=(root/f).read_text()
assert new.replace('tuning.tileContactInnerGlowIntensity,arrival?1:0,0','tuning.tileContactInnerGlowIntensity,0,0')==old,'Contact triggers or durations changed'
f='Assets/Scripts/Visuals/CompactPawnVisual.cs';old=(r/'baseline'/f).read_text();new=(root/f).read_text()
assert old.split('        float previousEmission=-1;',1)[0]==new.split('        BoardVisualTheme.SurfaceFinish lightingFinish;',1)[0], 'Pawn lifecycle, motion, pose or triggers changed'
assert old.split('        void TileContact',1)[1]==new.split('        void TileContact',1)[1], 'Pawn audio, contact dispatch or geometry changed'
f='Assets/Settings/Low_PipelineAsset.asset';old=(r/'baseline'/f).read_text();new=(root/f).read_text()
assert old.replace('  m_MainLightRenderingMode: 0','  m_MainLightRenderingMode: 1')==new,'Unexpected pipeline change'
for name in ['GraphiteSurface','NeonState']:
 f='Assets/Shaders/'+name+'.shader';old=(r/'baseline'/f).read_text();new=(root/f).read_text()
 assert old.count('Name "')==new.count('Name "'),'Extra shader pass'
out={'modified':changed,'created':created,'unchangedExistingFiles':sum(after.get(p)==v for p,v in before.items()),'boardGeometryScene40IndicesCollidersCameraPawnMovementBusinessEconomyNetworkDiceCockpitSelection':'byte-identical','pawnMotionAndAudioGeometryTriggers':'byte-identical','contactSystem':'byte-identical except final-arrival flag in unused texture channel','LowProfile':'one main light enabled, no shadow settings changed','shaderPassCounts':'unchanged'}
(r/'scope-validation.json').write_text(json.dumps(out,indent=2));(r/'after-inventory.json').write_text(json.dumps(after,indent=2))
diff=[]
for f in changed:diff.extend(difflib.unified_diff((r/'baseline'/f).read_text().splitlines(True),(root/f).read_text().splitlines(True),fromfile='before/'+f,tofile='after/'+f))
for f in created:diff.extend(difflib.unified_diff([],(root/f).read_text().splitlines(True),fromfile='/dev/null',tofile='after/'+f))
(r/'pass-2f-only.diff').write_text(''.join(diff));print(json.dumps(out,indent=2))
