"""Development-build-only camera recorder. No template/runtime asset changes."""
from pathlib import Path
root=Path(__file__).resolve().parents[3]
build=root/'Builds/CloseStrategic'
s=(build/'index.html').read_text()
s=s.replace('.then((unityInstance) => {','.then((unityInstance) => { window.cameraQA = unityInstance; document.getElementById("qaRecord").disabled = false; document.getElementById("qaState").textContent = "Unity ready";')
controls='''
<div style="position:fixed;left:8px;top:140px;padding:4px;background:#111922;color:#ddd;font:11px sans-serif;display:flex;gap:4px;align-items:center">
<label>Clip <input id="qaName" value="portrait-game" style="width:90px"></label>
<button id="qaRecord" disabled>Record</button><button id="qaStop" disabled>Stop</button><button id="qaFixtures">Fixtures</button><span id="qaState" role="status">Loading</span></div>
<script>
let recorder,chunks=[];
const qaState=document.getElementById('qaState');
window.cameraFixtureDone=()=>{qaState.textContent='Fixtures complete';if(recorder&&recorder.state==='recording')document.getElementById('qaStop').click();};
document.getElementById('qaFixtures').onclick=()=>{qaState.textContent='Fixtures running';window.cameraQA.SendMessage('[QA] StrategicCamera','RunAll');};
document.getElementById('qaRecord').onclick=()=>{
 const canvas=document.getElementById('unity-canvas');
 if(!canvas.captureStream || !window.MediaRecorder){qaState.textContent='Video unavailable';return;}
 const mime=['video/webm;codecs=vp9','video/webm;codecs=vp8','video/webm'].find(m=>MediaRecorder.isTypeSupported(m));
 chunks=[];recorder=new MediaRecorder(canvas.captureStream(30),{mimeType:mime,videoBitsPerSecond:3500000});
 recorder.ondataavailable=e=>{if(e.data.size)chunks.push(e.data);};
 recorder.onstop=async()=>{try{
  const blob=new Blob(chunks,{type:mime});const name=document.getElementById('qaName').value+'.webm';
  const response=await fetch('/__video?name='+encodeURIComponent(name),{method:'POST',body:blob});
  qaState.textContent=response.ok?'Saved '+name:'Save failed';
 }catch(e){qaState.textContent='Save failed';}};
 recorder.start(500);if(document.getElementById('qaName').value.endsWith('-game'))setTimeout(()=>{if(recorder&&recorder.state==='recording')document.getElementById('qaStop').click();},22000);window.cameraQA.SendMessage('[QA] StrategicCamera','CaptureMarker');
 document.getElementById('qaRecord').disabled=true;document.getElementById('qaStop').disabled=false;qaState.textContent='Recording';
};
document.getElementById('qaStop').onclick=()=>{window.cameraQA.SendMessage('[QA] StrategicCamera','EndCapture');recorder.stop();document.getElementById('qaStop').disabled=true;document.getElementById('qaRecord').disabled=false;qaState.textContent='Saving';};
</script>
'''
s=s.replace('</body>',controls+'</body>')
(build/'capture.html').write_text(s)
print('Created capture.html for this Development build only')
