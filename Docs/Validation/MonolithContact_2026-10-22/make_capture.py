from pathlib import Path
root=Path(__file__).resolve().parents[3]
build=root/'Builds/MonolithContact'
s=(build/'index.html').read_text()
# Capture the application's existing WebAudio destination, never a microphone.
audio='''<script>
const monoTaps=new WeakMap(),monoConnect=AudioNode.prototype.connect;
AudioNode.prototype.connect=function(destination,...args){
 const result=monoConnect.call(this,destination,...args);
 if(destination===this.context.destination){
  let tap=monoTaps.get(this.context);if(!tap){tap=this.context.createMediaStreamDestination();monoTaps.set(this.context,tap);window.monoTap=tap;}
  monoConnect.call(this,tap);
 }return result;
};
</script>'''
s=s.replace('</head>',audio+'</head>')
s=s.replace('.then((unityInstance) => {','.then((unityInstance) => { window.monoUnity=unityInstance; document.getElementById("record").disabled=false; document.getElementById("status").textContent="Unity ready";')
controls='''
<div style="position:fixed;left:8px;top:140px;background:#111922;color:#ddd;padding:4px;font:11px sans-serif">
<label>Clip <input id="clip" value="portrait-fixtures" style="width:105px"></label>
<button id="arm">Armer audio</button><button id="record" disabled>Record</button><button id="stop" disabled>Stop</button><button id="fixtures">Fixtures</button><button id="specimen">Spécimen</button><output id="status">Loading</output></div>
<script>
let videoRecorder,clockNode,clockGain;const status=document.getElementById('status');
document.getElementById('arm').onclick=()=>{window.monoUnity.SendMessage('[QA] Monolith','ArmAudio');status.textContent='Audio output initialized';};
document.getElementById('specimen').onclick=()=>{window.monoUnity.SendMessage('[QA] Monolith','ShowSpecimen');status.textContent='QA specimen 5x';};
async function save(recorder,stream,type,name){let chunks=[];recorder.ondataavailable=e=>{if(e.data.size)chunks.push(e.data);};
 recorder.onstop=async()=>{let result=await fetch('/__video?name='+name+'.webm',{method:'POST',body:new Blob(chunks,{type})});status.textContent=result.ok?'Saved '+name:'Save failed';};recorder.start(500);}
document.getElementById('record').onclick=()=>{
 const name=document.getElementById('clip').value,stream=document.getElementById('unity-canvas').captureStream(30);
 if(window.monoTap){const ctx=window.monoTap.context;ctx.resume();clockNode=ctx.createOscillator();clockGain=ctx.createGain();clockGain.gain.value=0;clockNode.connect(clockGain);clockGain.connect(window.monoTap);clockNode.start();}
 const tracks=window.monoTap?window.monoTap.stream.getAudioTracks():[];tracks.forEach(t=>stream.addTrack(t));
 const type=['video/webm;codecs=vp9,opus','video/webm;codecs=vp8,opus','video/webm'].find(m=>MediaRecorder.isTypeSupported(m));
 videoRecorder=new MediaRecorder(stream,{mimeType:type,videoBitsPerSecond:4000000});save(videoRecorder,stream,type,name);
 document.getElementById('record').disabled=true;document.getElementById('stop').disabled=false;
 status.textContent='Recording audio tracks='+tracks.length;window.monoUnity.SendMessage('[QA] Monolith','CaptureMarker');
 if(name.endsWith('-game'))setTimeout(()=>{if(videoRecorder.state==='recording')document.getElementById('stop').click();},25000);
};
document.getElementById('stop').onclick=()=>{window.monoUnity.SendMessage('[QA] Monolith','EndCapture');videoRecorder.stop();if(clockNode){clockNode.stop();clockNode.disconnect();clockGain.disconnect();clockNode=null;}document.getElementById('record').disabled=false;document.getElementById('stop').disabled=true;status.textContent='Saving';};
document.getElementById('fixtures').onclick=()=>{window.monoUnity.SendMessage('[QA] Monolith','RunAll');status.textContent='Fixtures running';};
window.monolithFixtureDone=()=>{if(videoRecorder&&videoRecorder.state==='recording')document.getElementById('stop').click();};
</script>'''
(build/'capture.html').write_text(s.replace('</body>',controls+'</body>'))
print('Generated QA recorder with WebAudio output tap')
