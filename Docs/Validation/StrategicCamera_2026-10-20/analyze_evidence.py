import json,math,re
from pathlib import Path
r=Path(__file__).resolve().parent
x=json.loads((r/'browser-validation.json').read_text());out={}
for key in ['portraitGame','fixtures','desktopGame','desktopFixtures']:
 d=x[key];f=d['frames'];moving=[q for q in f if q['state']=='MovementFocus' and not q['hold']]
 def events(items):
  return [(q['message'].strip(),float(re.search(r't=([0-9.]+)',q['message'])[1])) for q in items]
 states=events(d.get('states',[])); holds=events(d.get('holds',[]))
 pauses=[];returns=[];interruptedHolds=0
 for _,t in holds:
  later=[(s,u) for s,u in states if u>=t]
  if later and 'ReturnToOverview' in later[0][0]:pauses.append(round(later[0][1]-t,4))
  else:interruptedHolds+=1
 for s,t in states:
  if 'ReturnToOverview' in s:
   later=[u for q,u in states if u>=t and 'StrategicOverview' in q]
   if later:returns.append(round(later[0]-t,4))
 h=v=0
 for a,b in zip(f,f[1:]):
  if all(q['state']=='MovementFocus' and not q['hold'] for q in (a,b)):
   delta=[b['position'][c]-a['position'][c] for c in 'xyz']
   h+=abs((delta[0]-delta[2])/math.sqrt(2))
   v+=abs((delta[0]+delta[2])*math.sin(math.radians(40))/math.sqrt(2)+delta[1]*math.cos(math.radians(40)))
 held=[];group=[]
 for q in f+[{'hold':False}]:
  if q['hold']:group.append(q)
  elif group:
   held.append(len({(z['size'],*z['position'].values()) for z in group})==1);group=[]
 out[key]={'sampleCount':len(f),'movingSamples':len(moving),'movingOutsideReferenceViewport':sum(not q['visible'] for q in moving),'holdOutsideReferenceViewport':sum(not q['visible'] for q in f if q['hold']),
  'rotation40azimuth45':all(abs(q['rotation']['x']-40)<.001 and abs(q['rotation']['y']-45)<.001 for q in f),
  'holdsConstantPoseAndZoom':held,'observedHoldSeconds':pauses,'observedReturnSeconds':returns,'holdsInterruptedBeforeReturn':interruptedHolds,'traceVideoRange':[f[0]['vt'],f[-1]['vt']],
  'sampledCameraPlaneTravel':{'horizontal':round(h,4),'vertical':round(v,4)},
  'fixturePass':sum('CAMERA_CASE PASS' in q['message'] for q in d.get('cases',[])), 'fixtureFail':sum('CAMERA_CASE FAIL' in q['message'] for q in d.get('cases',[])), 'errors':d.get('errors',[])}
static=x.get('static',{});portrait=[v for k,v in static.items() if k.startswith('portrait')]
out['consultation']={'portraitProjectionExactlyEqual':all(v['projection']==portrait[0]['projection'] for v in portrait),'portraitEconomyExactlyEqual':all(v['economy']==portrait[0]['economy'] for v in portrait),'selected31':any(v['selected']==31 and v['inspectionName']=='Quantum AI Labs' for v in portrait),'statesRecorded':list(static)}
(r/'evidence-analysis.json').write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2))
