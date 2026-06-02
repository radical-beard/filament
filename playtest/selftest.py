"""Dev check of the survey submit pipeline: serve the survey on a fixed port,
write the submitted answers to the system temp directory, then exit. Drive it
with a browser (or Playwright) to verify render -> fill -> submit -> answers."""

import http.server
import json
import os
import sys
import tempfile
import threading

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from survey import render_survey
from preview import SAMPLE

PORT = 8799
OUT = os.path.join(tempfile.gettempdir(), "playtest_answers.json")


def main():
    page = render_survey(SAMPLE, "Self-test", "Fill + submit to verify the pipeline.").encode("utf-8")
    captured: dict = {}
    done = threading.Event()

    class Handler(http.server.BaseHTTPRequestHandler):
        def log_message(self, *a):
            pass

        def do_GET(self):
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.end_headers()
            self.wfile.write(page)

        def do_POST(self):
            n = int(self.headers.get("Content-Length", 0))
            try:
                captured.update(json.loads(self.rfile.read(n)).get("answers", {}))
            except (ValueError, AttributeError):
                pass
            self.send_response(200)
            self.end_headers()
            self.wfile.write(b'{"ok":true}')
            done.set()

    srv = http.server.HTTPServer(("127.0.0.1", PORT), Handler)
    threading.Thread(target=srv.serve_forever, daemon=True).start()
    print(f"serving on http://127.0.0.1:{PORT}/")
    done.wait(timeout=120)
    with open(OUT, "w", encoding="utf-8") as f:
        json.dump(captured, f)
    print("captured:", captured)


if __name__ == "__main__":
    main()
