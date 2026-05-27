# /// script
# requires-python = ">=3.11"
# dependencies = ["mcp>=1.2.0"]
# ///
"""Filament playtest MCP server.

Exposes one blocking tool, `playtest`, so an agent can hand a build to a human
and *wait* for structured feedback instead of passively handing off:

  1. launch the game (blocking, no timeout) — the human plays and closes it;
  2. pop a local web survey in the browser (built from the agent's questions);
  3. block until the human submits;
  4. return their answers to the agent.

The survey is plain web (not Godot), so it supports radio / multiselect / text /
textarea / number fields and conditional follow-ups, and renders after play.
"""

import http.server
import json
import os
import subprocess
import sys
import threading
import time
import webbrowser

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from survey import render_survey  # noqa: E402

from mcp.server.fastmcp import FastMCP  # noqa: E402

mcp = FastMCP("filament-playtest")


@mcp.tool()
def playtest(
    questions: list[dict],
    game_command: str = "",
    title: str = "Playtest feedback",
    intro: str = "",
) -> dict:
    """Run a blocking play-test session and collect structured feedback.

    Launches `game_command` (if given) and blocks with no timeout until the human
    closes it, then opens a web survey and blocks until they submit. Returns
    `{"answers": {question_id: value}}` (value is a string, number, or list for
    multiselect; follow-up answers are keyed by their own id).

    Each question is a dict:
      {
        "id": "dodge_iframes",
        "type": "radio" | "multiselect" | "text" | "textarea" | "number",
        "prompt": "Did dodging grant invincibility when expected?",
        "options": ["Yes", "Too early", "Too late", "No invuln"],  # radio/multiselect
        "required": true,
        "placeholder": "...",                                       # text/textarea/number
        "follow_ups": [   # optional, shown only when the answer matches show_when
          { "show_when": ["Too early", "Too late", "No invuln"],
            "question": { "id": "dodge_detail", "type": "textarea",
                          "prompt": "Describe what you experienced instead:" } }
        ]
      }

    Ask about things you cannot verify yourself — i-frame timing, input feel,
    hit confirmation — phrasing the hypothesis so a yes/no + detail is enough.
    """
    if game_command:
        subprocess.run(game_command, shell=True)  # blocks until the game window closes
    answers = _serve_and_collect(questions, title, intro)
    return {"answers": answers}


def _serve_and_collect(questions: list[dict], title: str, intro: str) -> dict:
    page = render_survey(questions, title, intro).encode("utf-8")
    answers: dict = {}
    submitted = threading.Event()

    class Handler(http.server.BaseHTTPRequestHandler):
        def log_message(self, *args):
            pass

        def do_GET(self):
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.end_headers()
            self.wfile.write(page)

        def do_POST(self):
            length = int(self.headers.get("Content-Length", 0))
            try:
                data = json.loads(self.rfile.read(length))
                answers.update(data.get("answers", {}))
            except (ValueError, AttributeError):
                pass
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.end_headers()
            self.wfile.write(b'{"ok":true}')
            submitted.set()

    server = http.server.HTTPServer(("127.0.0.1", 0), Handler)
    port = server.server_address[1]
    threading.Thread(target=server.serve_forever, daemon=True).start()

    webbrowser.open(f"http://127.0.0.1:{port}/")
    submitted.wait()      # blocks with no timeout until the human submits
    time.sleep(0.4)       # let the response flush to the browser
    server.shutdown()
    return answers


if __name__ == "__main__":
    mcp.run()
