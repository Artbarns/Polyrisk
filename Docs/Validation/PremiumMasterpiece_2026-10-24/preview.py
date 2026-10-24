#!/usr/bin/env python3
"""Start the existing local preview without tying its lifetime to an exec session.
No LaunchAgent, recurring task, account change, or game-source edit is installed.
"""
from pathlib import Path
import json,os,shutil,socket,subprocess,sys,time,urllib.request

REPORT=Path(__file__).resolve().parent
ROOT=REPORT.parents[2]
PRIVATE=Path('/tmp/polyrisk-premium-preview')
PRIVATE.mkdir(mode=0o700,exist_ok=True)
PRIVATE.chmod(0o700)

def listening(port):
    try:
        with socket.create_connection(('127.0.0.1',port),timeout=.3):return True
    except OSError:return False

def ready(url,marker):
    try:
        with urllib.request.urlopen(url,timeout=2) as response:
            return response.status==200 and marker in response.read(100000).decode('utf-8')
    except (OSError,ValueError):return False

def start(name,port,command,env,url,marker):
    pid=None
    if not listening(port):
        with (PRIVATE/(name+'.log')).open('ab') as log:
            child=subprocess.Popen(command,cwd=ROOT,env=env,stdin=subprocess.DEVNULL,
                stdout=log,stderr=subprocess.STDOUT,start_new_session=True,close_fds=True)
            pid=child.pid
    deadline=time.monotonic()+15
    while not ready(url,marker):
        if time.monotonic()>deadline:raise SystemExit('Preview unavailable: '+name+'; inspect '+str(PRIVATE/(name+'.log')))
        time.sleep(.2)
    return {'name':name,'port':port,'startedPid':pid,'url':url,'httpReady':True}

if __name__=='__main__':
    dotnet=shutil.which('dotnet') or '/usr/local/share/dotnet/dotnet'
    dll=ROOT/'Server/PolyRisk.LobbyServer/bin/Release/net10.0/PolyRisk.LobbyServer.dll'
    release=ROOT/'Builds/PremiumMasterpieceRelease'
    if not dll.is_file() or not (release/'index.html').is_file():
        raise SystemExit('Existing backend and completed Release build are required; nothing rebuilt implicitly.')
    env=os.environ.copy()
    env.update(POLYRISK_URLS='http://127.0.0.1:5097',POLYRISK_WEBGL_ROOT=str(release),
        POLYRISK_DATA_PATH=str(PRIVATE/'lobbies.json'))
    rows=[start('backend',5097,[dotnet,str(dll)],env,'http://127.0.0.1:5097/','PolyRisk')]
    rows.append(start('gallery',9007,[sys.executable,str(REPORT/'serve_webgl.py'),
        '--build-root',str(ROOT/'Builds'),'--port','9007','--api-upstream','http://127.0.0.1:5097'],
        os.environ.copy(),'http://127.0.0.1:9007/evidence/review.html','Graphite architectural'))
    result={'services':rows,'gallery':'http://127.0.0.1:9007/evidence/review.html',
        'play':'http://127.0.0.1:5097/','scope':'Loopback only; detached local preview, no public hosting or reboot service.'}
    (PRIVATE/'runtime.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result,indent=2))
