import json,math,re
from pathlib import Path
r=Path(__file__).resolve().parent
x=json.loads((r/'browser-validation.json').read_text());out={}
for key in ['portraitGame','fixtures','desktopGame','desktopFixtures']:
 d=x[key];f=sorted(d['frames'],key=lambda q:q['t']);moving=[q for q in f if q['state']=='MovementFocus' and not q['hold']]
 def events(items):return [(q['message'].strip(),float(re.search(r't=([0-9.]+)',q['message'])[1])) for q in items]
 states=events(d.get('states',[]));holds=events(d.get('holds',[]));pauses=[];returns=[];interrupted=0
 for _,t in holds:
  later=[(s,u) for s,u in states if u>=t]
  if later and 'ReturnToOverview' in later[0][0]:pauses.append(round(later[0][1]-t,4))
  else:interrupted+=1
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
 early=[]
 for s,t in states:
  if 'MovementFocus' not in s:continue
  prior=[q for q in f if q['t']<t and q['state']=='StrategicOverview']
  if not prior:continue
  before=prior[-1];after=[q for q in f if t<=q['t']<=t+.35 and q['state']=='MovementFocus']
  pan=[q['t']-t for q in after if abs((q['position']['x']-before['position']['x'])-(q['position']['z']-before['position']['z']))>.005]
  zoom=[q['t']-t for q in after if q['size']<before['size']-.005]
  early.append({'firstSampledPanDelay':round(min(pan),4) if pan else None,'firstSampledZoomDelay':round(min(zoom),4) if zoom else None})
 dice=[]
 for q in d.get('dice',[]):
  m=re.search(r'DICE_(BEGIN|RESULT|FADE) id=(-?\d+) actor=(\d+) values=(\d),(\d) up=(True|False) t=([0-9.]+)',q['message'])
  if not m:continue
  phase,ident,actor,a,b,up,t=m.groups();t=float(t)
  if phase=='BEGIN':dice.append({'id':int(ident),'actor':int(actor),'values':[int(a),int(b)],'begin':t})
  elif dice and dice[-1]['id']==int(ident):dice[-1][phase.lower()]=t;dice[-1]['resultUp']=up=='True'
 for item in dice:
  if 'result' in item:item['resultDelay']=round(item['result']-item['begin'],4)
  if 'fade' in item:
   item['movementLeadIn']=round(item['fade']-item['begin'],4)
   if 'result' in item:item['readableBeforeMovement']=round(item['fade']-item['result'],4)
   item['cameraStableBeforeMovement']=all(q['state']=='StrategicOverview' for q in f if item['begin']<=q['t']<item['fade'])
 out[key]={'sampleCount':len(f),'movingSamples':len(moving),'movingOutsideReferenceViewport':sum(not q['visible'] for q in moving),'holdOutsideReferenceViewport':sum(not q['visible'] for q in f if q['hold']),
 'rotation40azimuth45':all(abs(q['rotation']['x']-40)<.001 and abs(q['rotation']['y']-45)<.001 for q in f),'holdsConstantPoseAndZoom':held,
 'observedHoldSeconds':pauses,'observedReturnSeconds':returns,'holdsInterruptedBeforeReturn':interrupted,'earlyFollowSamples':early,
 'sampledCameraPlaneTravel':{'horizontal':round(h,4),'vertical':round(v,4)},
 'fixturePass':sum('CAMERA_CASE PASS' in q['message'] for q in d.get('cases',[])), 'fixtureFail':sum('CAMERA_CASE FAIL' in q['message'] for q in d.get('cases',[])),
 'restPawnPositionsPass':sum('CAMERA_CASE PASS rest pawn volume' in q['message'] for q in d.get('cases',[])),
 'resultFacesUpInAllVisibleResultSamples':all(q.get('facesUp') for q in f if q.get('diceVisible') and q.get('diceResult')),
 'dice':dice,'errors':d.get('errors',[]),'traceVideoRange':[f[0]['vt'],f[-1]['vt']] if f else []}
static=x.get('static',{});portrait=[v for k,v in static.items() if k.startswith('portrait')]
out['consultation']={'portraitProjectionExactlyEqual':all(v['projection']==portrait[0]['projection'] for v in portrait),'portraitEconomyExactlyEqual':all(v['economy']==portrait[0]['economy'] for v in portrait),'selected31':any(v['selected']==31 and v['inspectionName']=='Quantum AI Labs' for v in portrait),'statesRecorded':list(static)}
old=json.loads((r.parent/'StrategicCamera_2026-10-20/browser-validation.json').read_text())['static']
out['restComparison']={}
for key in ['portraitOverview','desktopOverview']:
 a=old[key]['projection'];b=static[key]['projection'];out['restComparison'][key]={'oldSize':a['orthographicSize'],'newSize':b['orthographicSize'],'linearGainPercent':round((a['orthographicSize']/b['orthographicSize']-1)*100,3),'areaGainPercent':round(((a['orthographicSize']/b['orthographicSize'])**2-1)*100,3),'collidersVisible':static[key]['collidersInside'],'indices':static[key]['indices']}
(r/'evidence-analysis.json').write_text(json.dumps(out,indent=2));print(json.dumps({k:{a:v for a,v in d.items() if a not in ['dice','earlyFollowSamples']} for k,d in out.items()},indent=2))
