#!/usr/bin/env python3
"""Serve one Unity WebGL build on distinct origins for local multiplayer QA."""
import argparse
import functools
import gzip
import http.server
import http.client
import json
import os
import pathlib
import threading
import urllib.parse


class WebGLHandler(http.server.SimpleHTTPRequestHandler):
    def send_head(self):
        original = self.path
        parsed = urllib.parse.urlsplit(original)
        binary = parsed.path.endswith((".wasm", ".data", ".js"))
        self.vary_encoding = binary
        accepted = self.headers.get("Accept-Encoding", "")
        accepts_gzip = any(part.strip().split(";")[0] == "gzip"
            and not any(parameter.strip() in ("q=0", "q=0.0", "q=0.00", "q=0.000")
                        for parameter in part.split(";")[1:])
            for part in accepted.split(","))
        if binary and accepts_gzip and pathlib.Path(self.translate_path(parsed.path) + ".gz").is_file():
            self.path = urllib.parse.urlunsplit(("", "", parsed.path + ".gz", parsed.query, ""))
        try:
            return super().send_head()
        finally:
            self.path = original

    def do_POST(self):
        upstream = self.server.api_upstream
        path = urllib.parse.urlsplit(self.path)
        if upstream is None or not (path.path == "/api/lobbies" or path.path.startswith("/api/lobbies/")):
            self.send_error(404)
            return
        try:
            length = int(self.headers.get("Content-Length", "0"))
        except ValueError:
            self.send_error(400, "Invalid request length")
            return
        if length < 0 or length > 384 * 1024:
            self.send_error(413)
            return
        if self.headers.get("Transfer-Encoding"):
            self.send_error(400, "Unsupported transfer encoding")
            return
        self.connection.settimeout(15)
        connection = http.client.HTTPConnection(upstream.hostname, upstream.port or 80, timeout=15)
        try:
            body = self.rfile.read(length)
            if len(body) != length:
                self.send_error(400, "Incomplete request")
                return
            connection.request("POST", self.path, body=body, headers={
                "Content-Type": self.headers.get("Content-Type", "application/json")})
            response = connection.getresponse()
            result = response.read()
            session_file = self.server.qa_session_file
            if session_file and path.path == "/api/lobbies" and response.status == 200:
                # Local QA only: retain host reconnect credentials outside the repo.
                fd = os.open(str(session_file), os.O_WRONLY | os.O_CREAT | os.O_TRUNC, 0o600)
                with os.fdopen(fd, "w") as output:
                    json.dump(json.loads(result), output)
            self.send_response(response.status)
            for name, value in response.getheaders():
                if name.lower() in ("content-type", "retry-after"):
                    self.send_header(name, value)
            self.send_header("Content-Length", str(len(result)))
            self.send_header("Cache-Control", "no-store")
            self.end_headers()
            self.wfile.write(result)
        except (OSError, http.client.HTTPException):
            self.send_error(502, "Game server unavailable")
        finally:
            connection.close()

    def guess_type(self, path):
        plain = path[:-3] if path.endswith(".gz") or path.endswith(".br") else path
        if plain.endswith(".wasm"):
            return "application/wasm"
        if plain.endswith(".data"):
            return "application/octet-stream"
        return super().guess_type(plain)

    def end_headers(self):
        if getattr(self, "vary_encoding", False):
            self.send_header("Vary", "Accept-Encoding")
        path = urllib.parse.urlsplit(self.path).path
        if path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        elif path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        super().end_headers()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", required=True, type=pathlib.Path)
    parser.add_argument("--ports", nargs="+", type=int, default=[8770])
    parser.add_argument("--bind", default="127.0.0.1")
    parser.add_argument("--api-upstream", help="Optional local lobby API for same-origin online play")
    parser.add_argument("--qa-session-file", type=pathlib.Path,
                        help="Optional local-only file for the host session response (mode 0600)")
    parser.add_argument("--precompress", action="store_true", help="Prepare gzip copies of Unity build assets once")
    args = parser.parse_args()
    directory = args.directory.resolve()
    if not directory.is_dir():
        parser.error("WebGL directory does not exist")
    if args.precompress:
        for asset in (directory / "Build").iterdir():
            if asset.suffix not in (".wasm", ".data", ".js"):
                continue
            compressed = pathlib.Path(str(asset) + ".gz")
            if compressed.exists() and compressed.stat().st_mtime >= asset.stat().st_mtime:
                continue
            compressed.write_bytes(gzip.compress(asset.read_bytes(), compresslevel=6, mtime=0))
    if len(set(args.ports)) != len(args.ports):
        parser.error("ports must be distinct")
    upstream = urllib.parse.urlsplit(args.api_upstream) if args.api_upstream else None
    if upstream is not None and (upstream.scheme != "http"
            or upstream.hostname not in ("127.0.0.1", "localhost")
            or upstream.path not in ("", "/") or upstream.query or upstream.fragment or upstream.username):
        parser.error("API upstream must be a local HTTP origin")
    servers = []
    try:
        handler = functools.partial(WebGLHandler, directory=str(directory))
        for port in args.ports:
            server = http.server.ThreadingHTTPServer((args.bind, port), handler)
            server.api_upstream = upstream
            server.qa_session_file = args.qa_session_file
            servers.append(server)
        for server in servers:
            threading.Thread(target=server.serve_forever, daemon=True).start()
            print("WebGL: http://{}:{}/".format(args.bind, server.server_port), flush=True)
        threading.Event().wait()
    except KeyboardInterrupt:
        pass
    finally:
        for server in servers:
            server.server_close()


if __name__ == "__main__":
    main()
