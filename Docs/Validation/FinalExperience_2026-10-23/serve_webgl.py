"""Local WebGL preview and browser-QA screenshot receiver. Binds loopback only."""
import argparse
import base64
import json
import re
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, urlsplit
import importlib.util

# Reuse the existing loopback-only authenticated API relay for local browser QA.
relay_path = Path(__file__).resolve().parents[3] / "Server/serve_webgl.py"
spec = importlib.util.spec_from_file_location("polyrisk_local_relay", relay_path)
relay = importlib.util.module_from_spec(spec)
spec.loader.exec_module(relay)

parser = argparse.ArgumentParser()
parser.add_argument("--build-root", required=True)
parser.add_argument("--port", type=int, default=8977)
parser.add_argument("--api-upstream")
parser.add_argument("--qa-session-file", type=Path)
args = parser.parse_args()
if args.api_upstream and not (urlsplit(args.api_upstream).scheme == "http" and urlsplit(args.api_upstream).hostname in ("localhost", "127.0.0.1")):
    parser.error("QA upstream must be loopback HTTP")
output = Path(__file__).resolve().parent


class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *a, **kw):
        super().__init__(*a, directory=args.build_root, **kw)

    def guess_type(self, path):
        return super().guess_type(path[:-3] if path.endswith(".gz") else path)

    def end_headers(self):
        if self.path.split("?", 1)[0].endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def do_GET(self):
        if self.path.startswith("/evidence/"):
            name = self.path.split("?",1)[0][len("/evidence/"):]
            path = output / name
            if Path(name).name != name or not path.is_file(): return self.send_error(404)
            body = path.read_bytes()
            requested = self.headers.get("Range", "")
            start, end = 0, len(body) - 1
            match = re.fullmatch(r"bytes=(\d+)-(\d*)", requested)
            if match:
                start = int(match[1]); end = min(end, int(match[2])) if match[2] else end
                if start > end or start >= len(body):
                    self.send_response(416); self.send_header("Content-Range", f"bytes */{len(body)}"); self.end_headers(); return
            self.send_response(206 if match else 200)
            self.send_header("Accept-Ranges", "bytes")
            if match: self.send_header("Content-Range", f"bytes {start}-{end}/{len(body)}")
            self.send_header("Content-Type", self.guess_type(str(path)))
            self.send_header("Content-Length", str(end-start+1))
            self.end_headers()
            self.wfile.write(body[start:end+1])
            return
        if self.path == "/__saved":
            body = '<!doctype html><meta charset="utf-8"><h1>Capture enregistrée</h1>'.encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return
        if self.path == "/__evidence":
            body = b'<meta charset="utf-8"><form method="post" action="/__evidence"><label>Evidence<textarea name="evidence"></textarea></label><button>Enregistrer</button></form>'
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return
        if self.path != "/__capture":
            return super().do_GET()
        body = b'<!doctype html><html lang="fr"><meta charset="utf-8"><title>Capture QA locale</title><form method="post" action="/__capture"><label>Nom<input name="name" value="portrait-compact.jpg"></label><label>Image<textarea name="image"></textarea></label><button>Enregistrer</button></form></html>'
        self.send_response(200)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_POST(self):
        if self.path == "/api/lobbies" or self.path.startswith("/api/lobbies/"):
            return relay.WebGLHandler.do_POST(self)
        if self.path.startswith("/__video?"):
            name = parse_qs(urlsplit(self.path).query).get("name", [""])[0]
            size = int(self.headers.get("Content-Length", "0"))
            if not name.endswith(".webm") or any(c not in "abcdefghijklmnopqrstuvwxyz0123456789-." for c in name) or not 0 < size <= 100_000_000:
                return self.send_error(400)
            (output / name).write_bytes(self.rfile.read(size))
            body = b"Video saved"
            self.send_response(200)
            self.send_header("Content-Type", "text/plain")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return
        if self.path == "/__evidence":
            size = int(self.headers.get("Content-Length", "0"))
            if not 0 < size <= 8_000_000: return self.send_error(413)
            fields = parse_qs(self.rfile.read(size).decode("utf-8"))
            data = json.loads(fields.get("evidence", [""])[0])
            if not isinstance(data, dict): return self.send_error(400)
            (output / "browser-validation.json").write_text(json.dumps(data,indent=2))
            self.send_response(303)
            self.send_header("Location", "/__saved")
            self.send_header("Content-Length", "0")
            self.end_headers()
            return
        if self.path != "/__capture":
            return self.send_error(404)
        size = int(self.headers.get("Content-Length", "0"))
        if not 0 < size <= 4_000_000:
            return self.send_error(413)
        fields = parse_qs(self.rfile.read(size).decode("utf-8"))
        name = fields.get("name", [""])[0]
        if not name.endswith(".jpg") or any(c not in "abcdefghijklmnopqrstuvwxyz0123456789-." for c in name):
            return self.send_error(400)
        data = base64.b64decode(fields.get("image", [""])[0], validate=True)
        if not data.startswith(b"\xff\xd8"):
            return self.send_error(400)
        (output / name).write_bytes(data)
        self.send_response(303)
        self.send_header("Location", "/__saved")
        self.send_header("Content-Length", "0")
        self.end_headers()


server = ThreadingHTTPServer(("127.0.0.1", args.port), Handler)
server.api_upstream = urlsplit(args.api_upstream) if args.api_upstream else None
server.qa_session_file = args.qa_session_file
server.serve_forever()
