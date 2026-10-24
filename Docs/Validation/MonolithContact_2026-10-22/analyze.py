import json,re,wave,struct,math
from pathlib import Path
r=Path(__file__).resolve().parent
x=json.loads((r/'browser-validation.json').read_text());out={}
for key,d in x.items():
 logs=d['logs']
 markers=[c for c in logs if c['message'].startswith('MONOLITH_CAPTURE_MARKER ')]
 if key=='desktop':
  starts=[c for c in logs if 'Bootstrapper: Deferred local game started.' in c['message']]
  cutoff=max(c['timestamp'] for c in starts)
 else:cutoff=max(c['timestamp'] for c in markers)
 logs=[c for c in logs if c['timestamp']>=cutoff]
 frames=[json.loads(c['message'].split('MONOLITH_FRAME ',1)[1]) for c in logs if c['message'].startswith('MONOLITH_FRAME ')]
 cases=[c['message'].strip() for c in logs if c['message'].startswith('MONOLITH_CASE ')]
 board=[json.loads(c['message'].split('BOARD_QA ',1)[1]) for c in logs if c['message'].startswith('BOARD_QA ')]
 result={'pass':sum(' PASS ' in c for c in cases),'fail':[c for c in cases if ' FAIL ' in c],'cases':cases,'errors':[c['message'] for c in logs if c['level']=='error'], 'board':board}
 if frames:
  start=frames[0]['t'];end=frames[-1]['t'];base=start-frames[0]['vt']
  contacts=[];tile=[];holds=[]
  for c in logs:
   message=c['message'];m=re.search(r'\bt=([\d.]+)',message)
   if not m:continue
   t=float(m[1]);vt=t-base
   if not start-.3<=t<=end+.3:continue
   if message.startswith('MONOLITH_CONTACT '):
    phase=re.search('MONOLITH_CONTACT (\\w+)',message)[1]
    contacts.append({'phase':phase,'t':t,'vt':round(vt,4),'audio':'audio=True' in message})
   if message.startswith('TILE_CONTACT '):tile.append({'index':int(re.search(r'index=(\d+)',message)[1]),'arrival':'arrival=True' in message,'t':t,'vt':round(vt,4)})
   if message.startswith('CAMERA_HOLD '):holds.append(t)
  locks=[c for c in contacts if c['phase']=='LOCK'];ticks=[c for c in contacts if c['phase']=='TICK'];sounds=[c for c in contacts if c['audio']]
  deltas=[round(min(abs(t-c['t']) for c in locks),4) for t in holds]
  result.update({'frames':len(frames),'recordedRange':[frames[0]['vt'],frames[-1]['vt']], 'contacts':contacts,'tileContacts':tile,'ticks':len(ticks),'locks':len(locks),'audioContacts':len(sounds),
   'lockHoldDeltaSeconds':deltas,'allTileContactsSharePawnAudioFrame':all(any(abs(t['t']-c['t'])<.001 for c in sounds) for t in tile),
   'contactHasActiveRenderedSample':all(any(abs(f['t']-t['t'])<.001 and f['tileActive'] for f in frames) for t in tile),
   'cameraAngles40and45':all(abs(f['rotation']['x']-40)<.001 and abs(f['rotation']['y']-45)<.001 for f in frames),
   'visualXZEqualsRootPlusOffset':all(abs(f['visual'][axis]-f['root'][axis]-f['offset'][axis])<.00001 for f in frames for axis in ['x','z']),
   'arrivalSamplesWithHold':sum(f['arriving'] and f['hold'] for f in frames)})
 out[key]=result
with wave.open(str(r/'portrait-fixtures-audio.wav')) as w:
 ch=w.getnchannels();rate=w.getframerate();n=w.getnframes();data=struct.unpack('<'+'h'*(n*ch),w.readframes(n))
peak=max(abs(v) for v in data)/32768;onsets=[];last=-1
for i,v in enumerate(data):
 if abs(v)<120:continue
 t=i//ch/rate
 if t-last>.08:onsets.append(t)
 last=t
expected=[c['vt'] for c in out['fixtures']['contacts'] if c['audio']]
out['audio']={'sampleRate':rate,'channels':ch,'duration':n/rate,'peakDbFS':20*math.log10(max(peak,1e-12)), 'clippedSamples':sum(abs(v)>=32767 for v in data),'sourceContacts':len(expected),'strongOnsetGroups':len(onsets),'onsets':onsets,'nearestOnsetSeconds':[round(min(abs(t-c) for t in onsets),4) for c in expected]}
(r/'evidence-analysis.json').write_text(json.dumps(out,indent=2))
print(json.dumps({k:{a:b for a,b in v.items() if a not in ['cases','board','contacts','tileContacts','onsets','nearestOnsetSeconds']} for k,v in out.items()},indent=2))
