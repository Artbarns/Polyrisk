import json,re,math,wave,struct,hashlib
from pathlib import Path
r=Path(__file__).resolve().parent
x=json.loads((r/'browser-validation.json').read_text());out={}
for key in ['fixtures','portraitGame','desktopGame']:
 d=x[key]
 f=d['frames'];start=f[0]['t'];end=f[-1]['t'];base=start-f[0]['vt']
 contacts=[]
 for c in d['contacts']:
  m=re.search(r'MONOLITH_CONTACT (\w+) actor=(\d+) seq=(\d+) waypoint=(\d+) crossings=(\d+) audio=(True|False) t=([\d.]+)',c['message'])
  if not m:continue
  phase,actor,seq,waypoint,crossings,audio,t=m.groups();t=float(t)
  if not start<=t<=end:continue
  contacts.append(dict(phase=phase,actor=int(actor),sequence=int(seq),waypoint=int(waypoint),crossings=int(crossings),audio=audio=='True',videoTime=round(t-base,4)))
 events=d.get('cameraEvents',[]);holdDelays=[]
 for event in events:
  if not event['message'].startswith('CAMERA_HOLD'):continue
  time=float(re.search(r't=([0-9.]+)',event['message'])[1])-base
  if not f[0]['vt']<=time<=f[-1]['vt']:continue
  locks=[c['videoTime'] for c in contacts if c['phase']=='LOCK' and c['videoTime']<=time]
  if locks and time-max(locks)<1:holdDelays.append(round(time-max(locks),4))
 out[key]={'samples':len(f),'recordedRange':[f[0]['vt'],f[-1]['vt']],
 'pass':sum(' PASS ' in c['message'] for c in d['cases']),'fail':[c['message'] for c in d['cases'] if ' FAIL ' in c['message']],
 'restPositionsPass':sum('PASS real monolith volume' in c['message'] for c in d['cases']),
 'cameraAngles40and45':all(abs(q['rotation']['x']-40)<.001 and abs(q['rotation']['y']-45)<.001 for q in f),
 'visualXZEqualsRootPlusOffset':all(abs(q['visual'][axis]-q['root'][axis]-q['offset'][axis])<.00001 for q in f for axis in ['x','z']),
 'recordedContacts':contacts,'ticks':sum(c['phase']=='TICK' for c in contacts),'locks':sum(c['phase']=='LOCK' for c in contacts),
 'audibleContacts':sum(c['audio'] for c in contacts),'lockToHistoricalCameraHoldSeconds':holdDelays,'errors':d['errors'],
 'overviewSizes':sorted({v['projection']['orthographicSize'] for v in d['board'] if v['cameraState']=='StrategicOverview'})}
with wave.open(str(r/'portrait-fixtures-audio.wav')) as w:
 ch=w.getnchannels();rate=w.getframerate();n=w.getnframes();data=struct.unpack('<'+'h'*(n*ch),w.readframes(n))
 peak=max(abs(v) for v in data)/32768;rms=math.sqrt(sum(v*v for v in data)/len(data))/32768
 # Strong onset groups; low codec/filter tails do not count as new contacts.
 onsets=[];last=-1
 for i,v in enumerate(data):
  if abs(v)<120:continue
  t=i//ch/rate
  if t-last>.08:onsets.append(t)
  last=t
 expected=[c['videoTime'] for c in out['fixtures']['recordedContacts'] if c['audio']]
 out['audio']={'file':'portrait-fixtures-audio.wav','duration':n/rate,'sampleRate':rate,'channels':ch,'peak':peak,
 'peakDbFS':20*math.log10(max(peak,1e-12)),'rms':rms,'clippedSamples':sum(abs(v)>=32767 for v in data),
 'sourceContacts':len(expected),'strongOnsetGroups':len(onsets),'strongOnsetTimes':onsets,
 'nearestStrongOnsetDistanceSeconds':[round(min(abs(t-c) for t in onsets),4) for c in expected]}
(r/'evidence-analysis.json').write_text(json.dumps(out,indent=2))
print(json.dumps({k:{a:b for a,b in v.items() if a not in ['recordedContacts','strongOnsetTimes','nearestStrongOnsetDistanceSeconds']} for k,v in out.items()},indent=2))
