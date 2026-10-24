from pathlib import Path
import json,subprocess,re,shutil,hashlib,math,wave,array
import imageio_ffmpeg
r=Path(__file__).resolve().parent;f=imageio_ffmpeg.get_ffmpeg_exe()
def run(args):return subprocess.run([f,'-v','error',*args],capture_output=True,check=True).stdout
names=['baseline-camera','baseline-contacts','after-a-contacts','after-b-contacts','after-b-camera','partie-b-coin']
output={};originals=r/'capture-originals';originals.mkdir(exist_ok=True)
for name in names:
 p=r/(name+'.webm');raw=originals/p.name
 if not raw.exists():shutil.copy2(p,raw)
 temp=r/(name+'-indexed.webm');run(['-y','-i',str(raw),'-map','0','-c','copy',str(temp)]);temp.replace(p)
 def signature(file):
  s=run(['-i',str(file),'-map','0:v:0','-vsync','0','-f','framemd5','-']).decode();video=[x.rsplit(',',1)[-1].strip() for x in s.splitlines() if not x.startswith('#')]
  audio=run(['-i',str(file),'-map','0:a:0','-f','md5','-']).decode().strip()
  return video,audio
 a,b=signature(raw),signature(p);assert a==b
 meta=subprocess.run([f,'-hide_banner','-i',str(p)],capture_output=True,text=True).stderr
 duration=re.search(r'Duration: (\d+):(\d+):(\d+\.\d+)',meta);secs=sum(float(x)*k for x,k in zip(duration.groups(),[3600,60,1]))
 output[name]={'frames':len(b[0]),'duration':secs,'nativeDecodedFramesUnchanged':True,'audioMD5':b[1],'bytes':p.stat().st_size,'codec':meta}
run(['-y','-i',str(r/'partie-b-coin.webm'),'-vn','-acodec','pcm_s16le',str(r/'partie-b-coin-audio.wav')])
with wave.open(str(r/'partie-b-coin-audio.wav')) as w:
 a=array.array('h',w.readframes(w.getnframes()));output['realAudio']={'sampleRate':w.getframerate(),'channels':w.getnchannels(),'peak':max(map(abs,a)),'clipped':sum(abs(x)>=32767 for x in a)}
source=r/'partie-b-coin.webm';clip=r/'partie-b-coin-short.webm'
def frame_lines(p):return [x for x in run(['-i',str(p),'-map','0:v:0','-vsync','0','-f','framemd5','-']).decode().splitlines() if not x.startswith('#')]
a,b=frame_lines(source),frame_lines(clip);assert a[:len(b)]==b
au=[run(['-i',str(p),'-t','14.5','-map','0:a:0','-f','md5','-']).decode().strip() for p in [source,clip]];assert au[0]==au[1]
output['presentationClip']={'source':source.name,'clip':clip.name,'unchangedInitialVideoFramesAndTimestamps':True,'frames':len(b),'first14point5SecondsAudioIdentical':True,'audioMD5':au[0],'edit':'end cut only, stream copy; no internal cut or speed change'}
(r/'media-validation.json').write_text(json.dumps(output,indent=2));print(json.dumps({k:{a:b for a,b in v.items() if a not in ['codec']} for k,v in output.items()},indent=2))
