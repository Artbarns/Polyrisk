#!/bin/zsh
cd -- "${0:A:h}"
polyrisk_project_root="${PWD:h:h:h}"
if ! curl -fsS --max-time 2 'http://127.0.0.1:9005/evidence/review.html' >/dev/null 2>&1; then
  nohup python3 "$PWD/serve_webgl.py" --build-root "$polyrisk_project_root/Builds" --port 9005 > "$PWD/server.log" 2>&1 < /dev/null &
  sleep 2
fi
open 'http://127.0.0.1:9005/evidence/review.html'
