#!/usr/bin/env python3
"""Local QA relay that can drop one response after the backend commits a snapshot."""
import argparse
import json
import os
import http.client
import http.server
import pathlib
import threading
import urllib.parse


class Relay(http.server.BaseHTTPRequestHandler):
    def relay(self):
        upstream = self.server.upstream
        body = self.rfile.read(int(self.headers.get("Content-Length", 0)))
        headers = {name: value for name, value in self.headers.items()
                   if name.lower() in ("content-type", "origin", "access-control-request-method",
                                       "access-control-request-headers")}
        is_command = self.command == "POST" and self.path.endswith("/game/commands")
        with self.server.fail_command_lock:
            fail_injected_command = is_command and self.server.fail_commands_remaining > 0
            if fail_injected_command:
                self.server.fail_commands_remaining -= 1
        fail_flag = self.server.fail_next_command_file
        if is_command and not fail_injected_command and fail_flag and fail_flag.exists():
            fail_flag.unlink()
            fail_injected_command = True
        if fail_injected_command:
            result = json.dumps({"error": "QA injected a transient failure before command commit."}).encode()
            self.send_response(503)
            origin = self.headers.get("Origin", "")
            parsed_origin = urllib.parse.urlsplit(origin)
            if parsed_origin.scheme in ("http", "https") and parsed_origin.hostname in (
                    "localhost", "127.0.0.1", "::1"):
                # Let the WebGL client observe the injected HTTP status instead of
                # having the browser mask it as a cross-origin transport failure.
                self.send_header("Access-Control-Allow-Origin", origin)
                self.send_header("Access-Control-Allow-Credentials", "true")
                self.send_header("Vary", "Origin")
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(result)))
            self.end_headers()
            self.wfile.write(result)
            print("QA: rejected one game command before forwarding ({} injected rejection(s) remain)".format(
                self.server.fail_commands_remaining), flush=True)
            return
        connection = http.client.HTTPConnection(upstream.hostname, upstream.port or 80, timeout=15)
        try:
            connection.request(self.command, self.path, body=body, headers=headers)
            response = connection.getresponse()
            result = response.read()
            session_file = self.server.session_file
            if session_file and self.command == "POST" and self.path == "/api/lobbies" and response.status == 200:
                # Explicit local QA option; never enabled on the public gateway.
                session = json.loads(result)
                fd = os.open(str(session_file), os.O_WRONLY | os.O_CREAT | os.O_TRUNC, 0o600)
                with os.fdopen(fd, "w") as output:
                    json.dump(session, output)
            flag = self.server.drop_next_file
            is_publication = self.command == "POST" and self.path.endswith("/game/state")
            if is_publication and response.status == 200 and flag.exists():
                flag.unlink()
                self.close_connection = True
                print("QA: dropped one committed publication response", flush=True)
                return
            self.send_response(response.status)
            for name, value in response.getheaders():
                if name.lower() not in ("content-length", "transfer-encoding", "connection"):
                    self.send_header(name, value)
            self.send_header("Content-Length", str(len(result)))
            self.end_headers()
            self.wfile.write(result)
        except (OSError, http.client.HTTPException):
            self.send_error(502, "QA upstream unavailable")
        finally:
            connection.close()

    do_POST = relay
    do_OPTIONS = relay
    do_GET = relay


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=0,
                        help="loopback port (0 chooses an available ephemeral port)")
    parser.add_argument("--upstream", default="http://127.0.0.1:5091")
    parser.add_argument("--drop-next-file", type=pathlib.Path, required=True)
    parser.add_argument("--fail-next-command-file", type=pathlib.Path,
                        help="if present, reject one game command with HTTP 503 before the backend sees it")
    parser.add_argument("--fail-next-commands", type=int, default=0,
                        help="reject exactly this many game commands with HTTP 503 before forwarding")
    parser.add_argument("--qa-session-file", type=pathlib.Path)
    args = parser.parse_args()
    if args.fail_next_commands < 0:
        parser.error("--fail-next-commands must be non-negative")
    upstream = urllib.parse.urlsplit(args.upstream)
    if upstream.scheme != "http" or upstream.hostname not in ("127.0.0.1", "localhost"):
        parser.error("QA upstream must be local HTTP")
    server = http.server.ThreadingHTTPServer(("127.0.0.1", args.port), Relay)
    server.session_file = args.qa_session_file
    server.upstream = upstream
    server.drop_next_file = args.drop_next_file
    server.fail_next_command_file = args.fail_next_command_file
    server.fail_commands_remaining = args.fail_next_commands
    server.fail_command_lock = threading.Lock()
    print("QA relay: http://127.0.0.1:{}".format(server.server_port), flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
