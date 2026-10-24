"""Local WebGL preview and browser-QA screenshot receiver. Binds loopback only."""
import argparse
import base64
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs

parser = argparse.ArgumentParser()
parser.add_argument("--build-root", required=True)
parser.add_argument("--port", type=int, default=8977)
args = parser.parse_args()
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
        if self.path == "/__saved":
            body = '<!doctype html><meta charset="utf-8"><h1>Capture enregistrée</h1>'.encode("utf-8")
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


ThreadingHTTPServer(("127.0.0.1", args.port), Handler).serve_forever()
