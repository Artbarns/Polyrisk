"""Encode actual WebGL recordings; trim only the QA restoration at the end."""
from pathlib import Path
import array,json,re,subprocess,sys
import imageio_ffmpeg
root=Path(__file__).resolve().parent
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
# Browser DOM command offset + measured runtime duration - 80ms guard before QA reset.
clips={'final-short-b':(.4218,2.35400390625),'final-long-b':(.6975,4.656982421875),'final-long-a':(.4153,5.2860107421875),'final-live-turns-b':None}
for name,timing in clips.items():
 if len(sys.argv)>1 and name not in sys.argv[1:]:continue
 source=root/(name+'.webm');dest=root/(name+'.mp4')
 if dest.exists():raise SystemExit('Refusing overwrite: '+str(dest))
 end=round(sum(timing)-.08,4) if timing else None
 args=[ffmpeg,'-hide_banner','-i',str(source)]
 if end is not None:args+=['-t',str(end)]
 args+=['-map','0:v:0','-map','0:a?','-c:v','libx264','-preset','fast','-crf','19','-pix_fmt','yuv420p','-vsync','vfr','-c:a','aac','-b:a','128k','-movflags','+faststart',str(dest)]
 run=subprocess.run(args,capture_output=True,text=True);(root/(name+'-encode.log')).write_text(run.stderr)
 if run.returncode:raise SystemExit(run.stderr[-2000:])
 probe=subprocess.run([ffmpeg,'-hide_banner','-i',str(dest),'-f','null','-'],capture_output=True,text=True)
 duration=re.search(r'Duration: (\d+):(\d+):(\d+\.\d+)',probe.stderr)
 seconds=sum(float(v)*m for v,m in zip(duration.groups(),[3600,60,1])) if duration else None
 raw=subprocess.run([ffmpeg,'-v','error','-i',str(dest),'-map','0:a:0','-f','s16le','-acodec','pcm_s16le','-'],capture_output=True,check=True).stdout
 samples=array.array('h');samples.frombytes(raw)
 result={'source':source.name,'mp4':dest.name,'seconds':seconds,'endTrimSeconds':end,'commandOffsetSeconds':timing[0] if timing else None,'fixtureRuntimeSeconds':timing[1] if timing else None,'decodePass':probe.returncode==0,'audio':{'samples':len(samples),'peak16bit':max(map(abs,samples),default=0),'clippedSamples':sum(abs(v)>=32767 for v in samples)},'retimed':False,'note':'Real-time capture. QA clips only omit the final pose restoration. Natural clip is uncut.'}
 (root/(name+'-media.json')).write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
