from pathlib import Path
import hashlib,json,re
r=Path(__file__).resolve().parent;root=r.parents[2]
before=json.loads((r/'before-inventory.json').read_text())
now={str(p.relative_to(root)):hashlib.sha256(p.read_bytes()).hexdigest() for d in ['Assets','ProjectSettings','Packages'] for p in (root/d).rglob('*') if p.is_file()}
changed=sorted(p for p,h in before.items() if now.get(p)!=h);created=sorted(set(now)-set(before))
allowed={'Assets/Scripts/Systems/BoardInteractionManager.cs','Assets/Scripts/Visuals/BoardContactFeedback.cs','Assets/Scripts/Board/BoardVisualTheme.cs','Assets/Settings/GraphiteFoundation/GraphiteTheme.asset','Assets/Scripts/Visuals/BoardSurfaceLighting.cs','Assets/Shaders/GraphiteSurface.shader'}
assert set(changed)==allowed,(changed,allowed)
assert set(created)=={f'Assets/Scripts/{folder}/{name}{ext}' for folder,name in [('Editor','PerceptualPolishValidation.cs'),('Debug','PerceptualPolishValidationDriver.cs')] for ext in ['', '.meta']},created
old=(r/'baseline/Assets/Scripts/Systems/BoardInteractionManager.cs').read_text();new=(root/'Assets/Scripts/Systems/BoardInteractionManager.cs').read_text()
for start,end in [('private void HandleVisualMovementFinished()', 'private void SetState('),('private bool CacheBoardBounds()',None)]:
 a=old[old.index(start):];b=new[new.index(start):]
 if end:a=a[:a.index(end)];b=b[:b.index(end)]
 assert a==b,start
for literal in ['movementZoomFactor = 1.14f','focusEntryDuration = .30f','arrivalHoldDuration = .25f','returnDuration = .375f','DefaultElevation = 40f']:
 assert literal in old and literal in new,literal
shaderOld=(r/'baseline/Assets/Shaders/GraphiteSurface.shader').read_text();shaderNew=(root/'Assets/Shaders/GraphiteSurface.shader').read_text()
assert re.findall(r'Name "(.*?)"',shaderOld)==re.findall(r'Name "(.*?)"',shaderNew)
assert [x.strip() for x in shaderOld.splitlines() if '#pragma' in x]==[x.strip() for x in shaderNew.splitlines() if '#pragma' in x]
# Existing A coefficients and geometry/palette retain their authored values.
oldTheme=(r/'baseline/Assets/Settings/GraphiteFoundation/GraphiteTheme.asset').read_text();newTheme=(root/'Assets/Settings/GraphiteFoundation/GraphiteTheme.asset').read_text()
assert oldTheme[:oldTheme.index('  surfaceTreatment:')]==newTheme[:newTheme.index('  surfaceTreatment:')]
def profile(text):
 return dict(re.findall(r'^    (\w+): (.+)$',text.split('  graphiteSatin:')[1].split('  reflectiveNeon:')[0],re.M))
a,b=profile(oldTheme),profile(newTheme);assert all(b[k]==v for k,v in a.items())
result={'modified':changed,'created':created,'unchangedExistingFiles':len(before)-len(changed),'restFramingCalibrationSelectionCockpitCallbacksPawnMovementEconomyNetworkDice':'unchanged; camera movement-only and visual envelope authorized','shaderPassesAndKeywords':'unchanged','AExistingCoefficientsAndGeometryPalette':'byte-identical'}
(r/'after-inventory.json').write_text(json.dumps(now,indent=2));(r/'scope-validation.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
