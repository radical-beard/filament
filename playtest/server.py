# /// script
# requires-python = ">=3.11"
# dependencies = ["mcp>=1.2.0"]
# ///
"""Filament playtest MCP server.

Exposes one blocking tool, `playtest`, so an agent can hand a build to a human
and *wait* for structured feedback instead of passively handing off:

  1. (optional) show a pre-play briefing page — what's new + controls — and block
     until the human clicks Start;
  2. launch the game (blocking, no timeout) — the human plays and closes it;
  3. pop a local web survey in the browser (built from the agent's questions);
  4. block until the human submits;
  5. return their answers to the agent.

The pages are plain web (not Godot): the briefing pops before play, the survey
after. The survey supports radio / multiselect / text / textarea / number fields
and conditional follow-ups.
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
from survey import render_briefing, render_survey  # noqa: E402

from mcp.server.fastmcp import FastMCP  # noqa: E402

mcp = FastMCP("filament-playtest")


@mcp.tool()
def playtest(
    questions: list[dict],
    game_command: str = "",
    title: str = "Playtest feedback",
    intro: str = "",
    briefing: dict | None = None,
) -> dict:
    """Run a blocking play-test session and collect structured feedback.

    Flow: (optional) show `briefing` and wait for Start -> launch `game_command`
    and block with no timeout until the human closes it -> open a web survey and
    block until they submit. Returns `{"answers": {question_id: value}}` (value is
    a string, number, or list for multiselect; follow-up answers keyed by their id).

    ALWAYS pass a `briefing` so the player knows the new verbs (otherwise they
    won't use them). It's a dict, all keys optional:
      { "whats_new": ["High-seas islands", "Kick attack", "Harpoon grapple"],
        "controls": [ {"keys": "WASD", "action": "Move"},
                      {"keys": "J", "action": "Kick"},
                      {"keys": "Space", "action": "Fire harpoon at a glowing spire"} ],
        "note": "free text" }

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
    if briefing:
        _serve_page(render_briefing(briefing))  # blocks until the human clicks Start

    if game_command:
        subprocess.run(game_command, shell=True)  # blocks until the game window closes

    result = _serve_page(render_survey(questions, title, intro))
    return {"answers": result.get("answers", {})}


def _serve_page(page_html: str) -> dict:
    """Serve a single page, open it in the browser, and block (no timeout) until it
    POSTs to /submit. Returns the posted JSON body as a dict."""
    page = page_html.encode("utf-8")
    posted: dict = {}
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
                body = json.loads(self.rfile.read(length))
                if isinstance(body, dict):
                    posted.update(body)
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
    submitted.wait()      # blocks with no timeout
    time.sleep(0.4)       # let the response flush to the browser
    server.shutdown()
    return posted


if __name__ == "__main__":
    mcp.run()
