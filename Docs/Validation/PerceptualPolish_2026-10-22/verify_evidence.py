from pathlib import Path
import json,re,math
r=Path(__file__).resolve().parent
read=lambda n:json.loads((r/n).read_text())
def timeline(file,prefix='MONOLITH_CAPTURE_MARKER '):
 data=read(file); marker=max((x for x in data if x['message'].startswith(prefix)),key=lambda x:x['timestamp'])
 return sorted((x for x in data if x['timestamp']>=marker['timestamp']),key=lambda x:x['timestamp']),float(marker['message'].split()[-1])
def objects(data,prefix):return [json.loads(x['message'][len(prefix):]) for x in data if x['message'].startswith(prefix)]
def timestamp(message):return float(re.search(r'\bt=([\d.]+)',message).group(1))
def mono_events(data,epoch):
 return {str(seq):[{'label':x['message'].split()[1]+' '+str(seq)+' / '+re.search(r'waypoint=(\d+)',x['message']).group(1),'time':timestamp(x['message'])-epoch,'message':x['message']} for x in data if x['message'].startswith('MONOLITH_CONTACT ') and f'seq={seq} ' in x['message'] and (' BEGIN ' in x['message'] or ' LOCK ' in x['message'] or ' TICK ' in x['message'])] for seq in range(1,7)}
result={};data={};epochs={};events={}
for key,file in [('baseline','baseline-contacts-browser.json'),('a','after-a-contacts-browser.json'),('b','after-b-contacts-browser.json'),('cameraBefore','baseline-camera-browser.json'),('cameraAfter','after-b-camera-browser.json'),('real','partie-b-coin-browser.json')]:
 data[key],epochs[key]=timeline(file);events[key]=mono_events(data[key],epochs[key]);(r/(key+'-timeline.json')).write_text(json.dumps(data[key],indent=2))
 result[key]={'monolithPass':sum(x['message'].startswith('MONOLITH_CASE PASS') for x in data[key]),'cameraPass':sum(x['message'].startswith('CAMERA_CASE PASS') for x in data[key]),'errors':[x for x in data[key] if x['level']=='error' or 'CASE FAIL' in x['message']]}
 assert not result[key]['errors'],(key,result[key]['errors'])
for key in ['baseline','a','b']:assert result[key]['monolithPass']==157
assert result['cameraAfter']['cameraPass']==75
contacts={}
for key in ['baseline','a','b','real']:
 cancellation=[timestamp(x['message']) for x in data[key] if x['message'].startswith('MONOLITH_CONTACT BEGIN ') and 'seq=7 ' in x['message']]
 cutoff=min(cancellation) if cancellation else float('inf')
 contacts[key]=[int(re.search(r'index=(\d+)',x['message']).group(1)) for x in data[key] if x['message'].startswith('TILE_CONTACT ') and timestamp(x['message'])<cutoff-.001]
 result[key]['cancellationTailContacts']=[x['message'] for x in data[key] if x['message'].startswith('TILE_CONTACT ') and timestamp(x['message'])>=cutoff-.001]

assert contacts['baseline']==contacts['a']==contacts['b'] and len(contacts['b'])==30
assert contacts['real']==list(range(10,20)),contacts['real']
# Source event clocks preserve the final contact/lock relationship.
for key in ['baseline','a','b','real']:
 final=[timestamp(x['message']) for x in data[key] if x['message'].startswith('TILE_CONTACT ') and 'arrival=True' in x['message']]
 locks=[timestamp(x['message']) for x in data[key] if x['message'].startswith('MONOLITH_CONTACT LOCK ')]
 assert len(final)==len(locks) and all(abs(a-b)<.001 for a,b in zip(final,locks)),(key,final,locks)
 result[key]['finalClockDeltaMs']=max(abs(a-b)*1000 for a,b in zip(final,locks))
# Compare the same ground reference and viewport, omitting entry/hold/reduced frames.
def anchor_stats(key):
 begins=[(timestamp(x['message']),int(re.search(r'seq=(\d+)',x['message']).group(1))) for x in data[key] if x['message'].startswith('MONOLITH_CONTACT BEGIN ')]
 frames=objects(data[key],'MONOLITH_FRAME ');pts=[]
 for f in frames:
  start,seq=max((b for b in begins if b[0]<=f['t']+.001),default=(f['t'],0))
  if not f['moving'] or f['reduced'] or f['hold'] or f['t']-start<=.45 or seq>5:continue
  yaw=math.radians(f['rotation']['y']);pitch=math.radians(f['rotation']['x']);right=(math.cos(yaw),0,-math.sin(yaw));up=(math.sin(yaw)*math.sin(pitch),math.cos(pitch),math.cos(yaw)*math.sin(pitch))
  v=(f['root']['x']-f['camera']['x'],3.954030990600586-f['camera']['y'],f['root']['z']-f['camera']['z']);scale=830/(2*f['size'])
  x=(215+sum(a*b for a,b in zip(v,right))*scale-8)/414;y=(415+sum(a*b for a,b in zip(v,up))*scale-244)/444
  pts.append((x,y))
 assert pts
 return {'samples':len(pts),'xMin':min(x for x,y in pts),'xMax':max(x for x,y in pts),'yMin':min(y for x,y in pts),'yMax':max(y for x,y in pts),'meanHorizontalDistanceFromAnchor':sum(abs(x-.5) for x,y in pts)/len(pts),'meanVerticalDistanceFromAnchor':sum(abs(y-.52) for x,y in pts)/len(pts)}
result['groundAnchor']={key:anchor_stats(key) for key in ['baseline','a','b']}
frames=objects(data['b'],'PERCEPTUAL_FRAME ');steady=[];begins=[timestamp(x['message']) for x in data['b'] if x['message'].startswith('MONOLITH_CONTACT BEGIN ')]
for f in frames:
 age=f['t']-max((t for t in begins if t<=f['t']+.001),default=f['t'])
 if f['state']=='MovementFocus' and not f['hold'] and not f['reduced'] and f['x']>=0 and age>.45:steady.append(f)
result['liveAnchorReadback']={'samples':len(steady),'xMin':min(f['x'] for f in steady),'xMax':max(f['x'] for f in steady),'yMin':min(f['y'] for f in steady),'yMax':max(f['y'] for f in steady)}
assert all(.43<f['x']<.57 and .46<f['y']<.58 for f in steady)
result['normalContactsMaxSimultaneous']=max(f['active'] for f in frames)
assert result['normalContactsMaxSimultaneous']<=2
inv=read('runtime-invariants.json');assert sum(x['message'].startswith('GRAPHITE_CASE PASS') for x in inv)==95;assert sum(x['message'].startswith('PERCEPTUAL_CASE PASS') for x in inv)==8;assert not any('CASE FAIL' in x['message'] for x in inv)
result['runtime']={'variantPass':95,'envelopePass':8,'firstSamples':[x['message'] for x in inv if x['message'].startswith('PERCEPTUAL_FIRST_SAMPLE')]}
inspect=read('inspection-cockpit-browser.json');poses=[objects(inspect[key],'GRAPHITE_QA ')[-1] for key in ['before','selection','cockpit']]
assert all(p['camera']==poses[0]['camera'] and p['angles']==poses[0]['angles'] and p['size']==poses[0]['size'] for p in poses)
board=objects(inspect['board'],'BOARD_QA ')[-1];assert board['selected']==31 and board['inspectionName']=='Quantum AI Labs' and board['rent']=='450'
expanded=read('cockpit-expanded-browser.json');expandedBoard=objects(expanded,'BOARD_QA ')[-1];expandedCamera=objects(expanded,'GRAPHITE_QA ')[-1]
assert expandedBoard['expanded'] and expandedBoard['projection']['orthographicSize']==poses[0]['size']
assert expandedCamera['camera']==poses[0]['camera'] and expandedCamera['angles']==poses[0]['angles'] and expandedCamera['size']==poses[0]['size']
result['cockpitActuallyExpanded']=True
result['inspectionCameraUnchanged']=True;result['realInspectedTile']=board
# Event synchronization uses native playback speed; starts and each contact can be aligned.
def paired(left,right):
 out=[]
 for seq,title in [(1,'Court — coin 10'),(2,'Court — coin 0'),(3,'Six cases — coin 20'),(4,'Douze cases — coins 30 et 0')]:
  a,b=events[left][str(seq)],events[right][str(seq)]
  assert len(a)==len(b)
  for i,(x,y) in enumerate(zip(a,b)):
   label=title+' · '+('départ' if i==0 else 'lock' if ' LOCK ' in x['message'] else 'contact '+str(i))
   out.append({'label':label,'left':max(0,x['time']-.04),'right':max(0,y['time']-.04)})
 return out
def camera_events(key):
 return [x for x in data[key] if x['message'].startswith('CAMERA_CASE PASS ') and (('two steps across corner 10 start' in x['message']) or ('long path corners 10 20 30 0 start' in x['message']))]
ca,cb=camera_events('cameraBefore'),camera_events('cameraAfter')
assert len(ca)==len(cb)==2
cmp={'contacts':{'left':'baseline-contacts.webm','right':'after-b-contacts.webm','leftLabel':'PASS 2F — B','rightLabel':'PASS 2F.1 — B','events':paired('baseline','b')},'ab':{'left':'after-a-contacts.webm','right':'after-b-contacts.webm','leftLabel':'A — Graphite Satin','rightLabel':'B — Reflective Neon','events':paired('a','b')},'camera':{'left':'baseline-camera.webm','right':'after-b-camera.webm','leftLabel':'Caméra PASS 2F — banc','rightLabel':'Caméra PASS 2F.1 — banc','events':[{'label':'Court — coin 10' if i==0 else 'Long — quatre coins','left':timestamp(a['message'])-epochs['cameraBefore'],'right':timestamp(b['message'])-epochs['cameraAfter']} for i,(a,b) in enumerate(zip(ca,cb))]}}
(r/'comparison-data.json').write_text(json.dumps(cmp,indent=2));(r/'evidence-validation.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2)[:6000])
