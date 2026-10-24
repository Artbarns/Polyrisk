"""Check evidence metadata against recorded browser logs; never mutates gameplay."""
from pathlib import Path
import json,re
r=Path(__file__).resolve().parent
read=lambda n:json.loads((r/n).read_text())
def records(logs,prefix):
 return [json.loads(x['message'][len(prefix):]) for x in logs if x['message'].startswith(prefix)]
verification=read('verification-browser.json')
checks=[x for x in verification if x['message'].startswith('GRAPHITE_CASE ')]
assert len(checks)==95 and all('GRAPHITE_CASE PASS' in x['message'] for x in checks)
comparisons={}
for size in ('portrait','desktop'):
 baseline=records(read('baseline-browser.json')[size],'BOARD_QA ')[-1]
 logs=read('comparison-'+size+'-raw.json');result={}
 for variant,treatment in [('a','GraphiteSatin'),('b','GraphiteReflectiveNeon')]:
  finish=records(logs[variant],'GRAPHITE_QA ')[-1]
  board=records(logs[variant],'BOARD_QA ')[-1]
  assert finish['treatment']==treatment and finish['quality']=='Low'
  assert (finish['width'],finish['height'])==((430,830) if size=='portrait' else (1200,900))
  assert finish['indices']==40 and finish['meshes']==4 and finish['lights']==1 and finish['realtimeProbes']==0
  assert abs(finish['size']-baseline['projection']['orthographicSize'])<1e-5
  assert board['economy']==baseline['economy'] and board['selected']==baseline['selected']==0
  assert board['projection']==baseline['projection'] and board['expanded']==False
  assert abs(finish['exposure']-.15)<1e-6 and abs(finish['bloom']-.08)<1e-6
  result[variant]=finish
 assert result['a']['camera']==result['b']['camera'] and result['a']['angles']==result['b']['angles']
 comparisons[size]=result
media=read('media-validation.json');fixture=media['variant-b-fixtures'];assert fixture['checksPass']==157 and fixture['checksFail']==0
assert len(fixture['tileContacts'])==30
for name in ('variant-b-fixtures','variant-b-game'):
 assert media[name]['remuxDecodedPixelsAndAudioUnchanged'] and not media[name]['errors']
 timeline=read(name+'-timeline.json')
 contacts=[x['message'] for x in timeline if x['message'].startswith('TILE_CONTACT')]
 locks=[x['message'] for x in timeline if x['message'].startswith('MONOLITH_CONTACT LOCK')]
 times=lambda msgs:[float(re.search(r'\bt=([\d.]+)',x).group(1)) for x in msgs]
 final=times([x for x in contacts if 'arrival=True' in x]);locked=times(locks)
 assert len(final)==len(locked) and max(abs(a-b) for a,b in zip(final,locked))<.001
 media[name]['maxTileLockTimeDeltaMs']=max(abs(a-b)*1000 for a,b in zip(final,locked))
 assert sum('MONOLITH_CONTACT TICK' in x for x in media[name]['pawnContacts'])+len(locks)==len(contacts)
perf=records(read('performance-browser.json'),'GRAPHITE_MEASURE ')
latest={}
for p in perf:latest[p['quality']+'/'+p['treatment']]=p
mobile=records(read('selection-mobile-browser.json'),'BOARD_QA ')[-1]
assert mobile['selected']==31 and mobile['inspectionName']=='Quantum AI Labs' and mobile['rent']=='450'
output={'runtimeChecksPass':95,'fixtureChecksPass':157,'baselineCameraStateExposureSame':comparisons,'media':media,'steadyPerformanceWindows':latest,'allPerformanceWindows':perf,'mobileSelectionAndInspection':mobile}
(r/'evidence-validation.json').write_text(json.dumps(output,indent=2))
print('PASS: 95 A/B checks; 157 fixture checks; matching baseline camera, initial economy and exposure; 30 fixture contacts; same-frame tile/lock; mobile tile31 inspection; Low A/B comparisons.')
