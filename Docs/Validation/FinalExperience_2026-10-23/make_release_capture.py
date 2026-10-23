"""Local-only recorder for the unmodified Release player; no gameplay controls or state writes."""
from pathlib import Path
root=Path(__file__).resolve().parents[3]
build=root/'Builds/FinalExperience'
s=(build/'index.html').read_text()
audio='''<script>
const taps=new WeakMap(),originalConnect=AudioNode.prototype.connect;
AudioNode.prototype.connect=function(destination,...args){const result=originalConnect.call(this,destination,...args);
 if(destination===this.context.destination){let tap=taps.get(this.context);if(!tap){tap=this.context.createMediaStreamDestination();taps.set(this.context,tap);window.gameTap=tap;}originalConnect.call(this,tap);}return result;};
</script>'''
controls='''<div id="captureTools" style="position:fixed;top:138px;left:8px;background:#111922;color:#ddd;padding:5px;font:11px sans-serif;z-index:2000">
<label>Clip<input id="clip" value="release-multiplayer-game" style="width:170px"></label><button id="record">Record</button><button id="stop" disabled>Stop</button><button id="hide">Masquer (H)</button><output id="captureStatus">Release sans commandes QA</output></div>
<script>
let recorder;const status=document.getElementById('captureStatus');
document.getElementById('record').onclick=()=>{const name=document.getElementById('clip').value,stream=document.getElementById('unity-canvas').captureStream(30);
if(window.gameTap){window.gameTap.context.resume();window.gameTap.stream.getAudioTracks().forEach(t=>stream.addTrack(t));}
const mime=['video/webm;codecs=vp9,opus','video/webm;codecs=vp8,opus','video/webm'].find(m=>MediaRecorder.isTypeSupported(m));
let chunks=[];recorder=new MediaRecorder(stream,{mimeType:mime,videoBitsPerSecond:4000000});recorder.ondataavailable=e=>{if(e.data.size)chunks.push(e.data);};
recorder.onstop=async()=>{let result=await fetch('/__video?name='+name+'.webm',{method:'POST',body:new Blob(chunks,{type:mime})});status.textContent=result.ok?'Saved '+name:'Save failed';};
recorder.start(500);status.textContent='Recording audio tracks='+stream.getAudioTracks().length;document.getElementById('record').disabled=true;document.getElementById('stop').disabled=false;
setTimeout(()=>{if(recorder.state==='recording')document.getElementById('stop').click();},35000);};
document.getElementById('stop').onclick=()=>{recorder.stop();document.getElementById('record').disabled=false;document.getElementById('stop').disabled=true;status.textContent='Saving';};
document.getElementById('hide').onclick=()=>document.getElementById('captureTools').style.visibility='hidden';document.addEventListener('keydown',e=>{if(e.key.toLowerCase()==='h')document.getElementById('captureTools').style.visibility='visible';});
</script>'''
(build/'capture.html').write_text(s.replace('</head>',audio+'</head>').replace('</body>',controls+'</body>'))
print('Release recorder generated locally; excluded from deployment bundle.')
