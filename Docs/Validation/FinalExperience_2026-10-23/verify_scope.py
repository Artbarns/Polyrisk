"""Compare this release to the user's complete pre-existing working-copy baseline."""
from pathlib import Path
import hashlib,json
root=Path(__file__).resolve().parents[3]
report=Path(__file__).resolve().parent
before=json.loads((report/'baseline-manifest.json').read_text())
allowed={
 'Assets/Scripts/Systems/BoardInteractionManager.cs',
 'Assets/WebGLTemplates/PolyRiskResponsive/index.html',
 'Assets/WebGLTemplates/PolyRiskResponsive/TemplateData/style.css',
 'Assets/Settings/GraphiteFoundation/GraphiteTheme.asset',
 'Assets/Scripts/Board/BoardVisualTheme.cs',
 'Assets/Scripts/Visuals/BoardSurfaceLighting.cs',
 'Assets/Shaders/GraphiteSurface.shader',
 'Assets/Shaders/NeonState.shader',
 'Server/PolyRisk.LobbyServer/Program.cs',
 'Assets/UI/Styles/Theme.uss',
}
changed=[];unexpected=[];missing=[]
for row in before:
 p=root/row['path']
 if not p.is_file():missing.append(row['path']);continue
 if hashlib.sha256(p.read_bytes()).hexdigest()!=row['sha256']:
  changed.append(row['path'])
  if row['path'] not in allowed:unexpected.append(row['path'])
result={'baselineFiles':len(before),'unchanged':len(before)-len(changed)-len(missing),'modified':changed,'unexpected':unexpected,'missing':missing,
 'rulesEngineUnchanged':not any('RulesEngine' in x for x in changed+missing),
 'scopePass':not unexpected and not missing}
(report/'scope-validation.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result,indent=2))
assert result['scopePass']
