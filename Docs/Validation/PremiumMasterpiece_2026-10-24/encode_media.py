"""Package real canvas/WebAudio captures for playback; no retiming or invented frames."""
from pathlib import Path
import subprocess,json,re,sys,wave,array
import imageio_ffmpeg
r=Path(__file__).resolve().parent
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe();results=[]
for name in sys.argv[1:]:
 src=r/(name+'.webm');dst=r/(name+'.mp4')
 if not src.is_file():raise SystemExit('Missing real source: '+str(src))
 args=[ffmpeg,'-hide_banner','-i',str(src),'-map','0:v:0','-map','0:a?','-c:v','libx264','-preset','fast','-crf','19','-pix_fmt','yuv420p','-vsync','vfr','-c:a','aac','-b:a','128k','-movflags','+faststart',str(dst)]
 if dst.exists():raise SystemExit('Output already exists, refusing overwrite: '+str(dst))
 run=subprocess.run(args,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
 (r/(name+'-encode.log')).write_text(run.stderr)
 if run.returncode:raise SystemExit(run.stderr[-2000:])
 probe=subprocess.run([ffmpeg,'-hide_banner','-i',str(dst),'-f','null','-'],stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True)
 duration=re.search(r'Duration: (\d+):(\d+):(\d+\.\d+)',probe.stderr)
 sec=sum(float(v)*m for v,m in zip(duration.groups(),[3600,60,1])) if duration else None
 audio=None
 if 'Audio:' in probe.stderr:
  raw=subprocess.run([ffmpeg,'-v','error','-i',str(dst),'-map','0:a:0','-f','s16le','-acodec','pcm_s16le','-'],stdout=subprocess.PIPE,check=True).stdout
  samples=array.array('h');samples.frombytes(raw)
  audio={'samples':len(samples),'peak16bit':max((abs(x) for x in samples),default=0),'clippedSamples':sum(abs(x)>=32767 for x in samples)}
 info={'source':src.name,'mp4':dst.name,'seconds':sec,'decodePass':probe.returncode==0,'audio':audio,'retimed':False,'encode':'H264/AAC, VFR preserved, faststart'}
 results.append(info)
 (r/(name+'-media.json')).write_text(json.dumps(info,indent=2)+'\n')
print(json.dumps(results,indent=2))
