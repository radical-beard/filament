# filament · playtest

A **blocking** play-test harness so an agent can hand a build to a human and
*wait* for structured feedback instead of passively handing off.

## Flow

`playtest` (MCP tool in `server.py`):
1. launches the game (`game_command`) and **blocks with no timeout** while the human plays and closes it;
2. opens a local **web survey** in the browser (built from the agent's questions — not Godot, so it pops up after play);
3. blocks until the human submits;
4. returns `{"answers": {question_id: value}}` to the agent.

The survey (`survey.py`) supports **radio, multiselect, text, textarea, number**, and conditional follow-ups (a field shown only when a prior answer matches).

## Using it (from an agent)

Call the `playtest` tool with `game_command`, `questions`, and optional `title`/`intro`.
The question schema is documented in `server.py`'s docstring. Ask about things you
*can't* verify yourself — i-frame timing, input feel, hit confirmation — phrased so a
human yes/no + detail closes the loop. Example:

```json
{ "id": "dodge_iframes", "type": "radio",
  "prompt": "Did dodging grant invincibility when expected?",
  "options": ["Yes", "Too early", "Too late", "No invuln"], "required": true,
  "follow_ups": [ { "show_when": ["Too early","Too late","No invuln"],
    "question": { "id": "dodge_detail", "type": "textarea",
                  "prompt": "Describe what you experienced instead:" } } ] }
```

## Register (needs a Claude Code restart to load)

```json
"filament-playtest": {
  "command": "uv",
  "args": ["run", "--script", "/Users/jaaaacob/workplace/filament/playtest/server.py"]
}
```

## Dev helpers

- `preview.py` — render a sample survey (all field types) to a temp HTML file for screenshotting.
- `selftest.py` — serve the survey on `:8799` and write a submitted result to `/tmp/playtest_answers.json` (drive with a browser/Playwright to verify the pipeline).
