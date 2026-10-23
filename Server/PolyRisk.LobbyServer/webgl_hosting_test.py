#!/usr/bin/env python3
"""Isolated checks of optional same-origin hosting, MIME/compression and the unchanged lobby protocol."""
import gzip
import json
import os
import pathlib
import socket
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request

ROOT = pathlib.Path(__file__).resolve().parent
DLL = ROOT / "bin/Release/net10.0/PolyRisk.LobbyServer.dll"


def main():
    checks = 0
    def check(condition, label):
        nonlocal checks
        assert condition, label
        checks += 1

    with tempfile.TemporaryDirectory(prefix="polyrisk-hosting-") as directory:
        temporary = pathlib.Path(directory)
        webgl = temporary / "webgl"
        build = webgl / "Build"
        build.mkdir(parents=True)
        (webgl / "index.html").write_text("<!doctype html><title>PolyRisk hosting validation</title>")
        digest = "a" * 32
        wasm = b"\x00asm\x01\x00\x00\x00"
        (build / (digest + ".wasm")).write_bytes(wasm)
        (build / (digest + ".wasm.gz")).write_bytes(gzip.compress(wasm, mtime=0))
        (build / (digest + ".data")).write_bytes(b"Unity fixture data")
        (build / (digest + ".framework.js.br")).write_bytes(b"header check only")
        (build / "loader.js").write_text("/* loader */")
        with socket.socket() as sock:
            sock.bind(("127.0.0.1", 0))
            port = sock.getsockname()[1]
        base = f"http://127.0.0.1:{port}"
        env = os.environ.copy()
        env.pop("POLYRISK_URLS", None)
        env.update(PORT=str(port), POLYRISK_WEBGL_ROOT=str(webgl),
                   POLYRISK_DATA_PATH=str(temporary / "private-data/lobbies.json"),
                   POLYRISK_ALLOWED_ORIGINS="https://game.example.test")
        with (temporary / "server.log").open("w") as log:
            process = subprocess.Popen(["dotnet", str(DLL)], env=env, cwd=ROOT, stdout=log, stderr=log)
            try:
                for _ in range(100):
                    if process.poll() is not None:
                        raise AssertionError("Hosting server exited before startup")
                    try:
                        with urllib.request.urlopen(base + "/", timeout=3) as response:
                            index = response.read()
                            break
                    except (OSError, urllib.error.URLError):
                        time.sleep(.1)
                else:
                    raise AssertionError("Hosting startup timed out")
                check(b"PolyRisk hosting validation" in index, "root serves WebGL, not the API health JSON")
                with urllib.request.urlopen(base + "/", timeout=5) as response:
                    check(response.headers["Cache-Control"] == "no-cache", "index revalidation")
                    check(response.headers["X-Content-Type-Options"] == "nosniff", "security header preserved")
                    check(response.headers["X-Frame-Options"] == "DENY", "framing protection preserved")
                assets = [(digest + ".wasm", "application/wasm", None),
                          (digest + ".data", "application/octet-stream", None),
                          (digest + ".wasm.gz", "application/wasm", "gzip"),
                          (digest + ".framework.js.br", "text/javascript", "br")]
                for name, content_type, encoding in assets:
                    with urllib.request.urlopen(base + "/Build/" + name, timeout=5) as response:
                        check(response.headers["Content-Type"].split(";")[0] == content_type, name + " MIME")
                        check(response.headers.get("Content-Encoding") == encoding, name + " encoding")
                        check("immutable" in response.headers["Cache-Control"], name + " cache")
                        if encoding == "gzip":
                            check(gzip.decompress(response.read()) == wasm, "gzip roundtrip")
                request = urllib.request.Request(base + "/Build/" + digest + ".wasm", headers={"Range": "bytes=0-3"})
                with urllib.request.urlopen(request, timeout=5) as response:
                    check(response.status == 206 and response.read() == wasm[:4], "range response")
                with urllib.request.urlopen(base + "/Build/loader.js", timeout=5) as response:
                    check(response.headers["Cache-Control"] == "no-cache", "nonhashed loader revalidation")
                for path in ("/data/lobbies.json", "/../private-data/lobbies.json", "/Program.cs", "/.env"):
                    try:
                        urllib.request.urlopen(base + path, timeout=5)
                    except urllib.error.HTTPError as error:
                        check(error.code == 404, path + " not public")
                    else:
                        raise AssertionError(path + " unexpectedly public")
                for origin, allowed in [("https://game.example.test", True), ("https://attacker.example.test", False)]:
                    request = urllib.request.Request(base + "/api/lobbies", method="OPTIONS", headers={
                        "Origin": origin, "Access-Control-Request-Method": "POST"})
                    with urllib.request.urlopen(request, timeout=5) as response:
                        check((response.headers.get("Access-Control-Allow-Origin") == origin) == allowed, "CORS origin restriction")
                subprocess.run([sys.executable, str(ROOT / "smoke_test.py"), base], check=True)
                check(True, "six-player protocol through same-origin hosting")
            finally:
                process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=5)
    print(f"WEBGL SAME-ORIGIN HOSTING PASS: {checks} checks; isolated data, no public deployment implied")


if __name__ == "__main__":
    main()
