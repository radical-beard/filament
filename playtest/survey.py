"""Renders the playtest web pages as self-contained HTML.

- `render_briefing(briefing)` — the *pre-play* page: what's new + controls + a
  "Start" button. Shown before the game launches so the player knows the verbs.
- `render_survey(questions)` — the *post-play* feedback form.

A question is a dict:
    {
      "id": "dodge_iframes",
      "type": "radio" | "multiselect" | "text" | "textarea" | "number",
      "prompt": "Did dodging grant invincibility when expected?",
      "options": ["Yes", "Too early", "Too late", "No invuln"],   # radio/multiselect
      "required": True,
      "placeholder": "...",                                        # text/textarea/number
      "follow_ups": [                                              # optional, conditional
         { "show_when": ["Too early", "Too late", "No invuln"],
           "question": { "id": "dodge_detail", "type": "textarea",
                         "prompt": "Describe what you experienced instead:" } }
      ]
    }

A briefing is a dict (all keys optional):
    { "whats_new": ["...", "..."],
      "controls": [{"keys": "WASD", "action": "Move"}, {"keys": "Space", "action": "Harpoon"}],
      "note": "free text" }

Both render functions are pure (data -> HTML) so they can be screenshot-tested
without launching anything.
"""

import html
import json


def render_survey(questions, title="Playtest feedback", intro=""):
    page = _SURVEY_TEMPLATE
    page = page.replace("__STYLE__", _STYLE)
    page = page.replace("__QUESTIONS_JSON__", json.dumps(questions))
    page = page.replace("__TITLE__", html.escape(title))
    page = page.replace("__INTRO__", html.escape(intro))
    return page


def render_briefing(briefing, title="Before you play", intro="What's new in this build, and how to play it:"):
    briefing = briefing or {}
    whats = briefing.get("whats_new") or []
    controls = briefing.get("controls") or []
    note = briefing.get("note") or ""

    whats_block = ""
    if whats:
        items = "".join(f"<li>{html.escape(str(w))}</li>" for w in whats)
        whats_block = f'<div class="question"><label class="prompt">What’s new</label><ul class="whatsnew">{items}</ul></div>'

    controls_block = ""
    if controls:
        rows = "".join(
            f'<div class="kv"><span class="key">{html.escape(str(c.get("keys", "")))}</span>'
            f'<span>{html.escape(str(c.get("action", "")))}</span></div>'
            for c in controls
        )
        controls_block = f'<div class="question"><label class="prompt">Controls</label><div class="controls">{rows}</div></div>'

    note_block = ""
    if note:
        note_block = f'<div class="question"><p class="intro" style="margin:0">{html.escape(str(note))}</p></div>'

    page = _BRIEFING_TEMPLATE
    page = page.replace("__STYLE__", _STYLE)
    page = page.replace("__TITLE__", html.escape(title))
    page = page.replace("__INTRO__", html.escape(intro))
    page = page.replace("__WHATS_NEW__", whats_block)
    page = page.replace("__CONTROLS__", controls_block)
    page = page.replace("__NOTE__", note_block)
    return page


_STYLE = r"""
  :root {
    --bg: #12161d; --card: #1c232e; --card2: #232c39; --line: #313c4c;
    --text: #e7ebf1; --muted: #93a0b3; --accent: #f0a73a; --accent2: #6ad0c8;
    --bad: #e0566a;
  }
  * { box-sizing: border-box; }
  body {
    margin: 0; background: var(--bg); color: var(--text);
    font: 16px/1.5 system-ui, -apple-system, Segoe UI, Roboto, sans-serif;
    padding: 40px 16px 120px;
  }
  .wrap { max-width: 720px; margin: 0 auto; }
  h1 { font-size: 26px; margin: 0 0 4px; letter-spacing: 0.2px; }
  .intro { color: var(--muted); margin: 0 0 28px; }
  .question {
    background: var(--card); border: 1px solid var(--line); border-radius: 12px;
    padding: 18px 20px; margin: 0 0 16px;
  }
  .prompt { display: block; font-weight: 600; margin-bottom: 12px; }
  .req { color: var(--accent); }
  .options { display: flex; flex-direction: column; gap: 8px; }
  .opt {
    display: flex; align-items: center; gap: 10px; padding: 9px 12px;
    background: var(--card2); border: 1px solid var(--line); border-radius: 9px;
    cursor: pointer; transition: border-color .12s, background .12s;
  }
  .opt:hover { border-color: var(--accent); }
  .opt input { accent-color: var(--accent); width: 17px; height: 17px; }
  input[type=text], input[type=number], textarea {
    width: 100%; background: var(--card2); color: var(--text);
    border: 1px solid var(--line); border-radius: 9px; padding: 11px 13px;
    font: inherit; resize: vertical;
  }
  input:focus, textarea:focus { outline: none; border-color: var(--accent2); }
  .followups { margin-top: 12px; padding-left: 14px; border-left: 2px solid var(--accent); }
  .followup .question { background: var(--card2); margin-bottom: 10px; }
  .hidden { display: none; }
  .invalid { border-color: var(--bad) !important; }
  .err { color: var(--bad); font-size: 13px; margin-top: 8px; display: none; }
  .bar {
    position: fixed; left: 0; right: 0; bottom: 0; padding: 16px;
    background: linear-gradient(transparent, var(--bg) 30%); text-align: center;
  }
  button {
    background: var(--accent); color: #1a1205; border: 0; border-radius: 10px;
    font: 600 16px/1 inherit; padding: 14px 34px; cursor: pointer;
  }
  button:hover { filter: brightness(1.07); }
  .thanks, .launching { text-align: center; padding: 60px 0; }
  .thanks h2, .launching h2 { color: var(--accent2); }
  .whatsnew { margin: 0; padding-left: 20px; }
  .whatsnew li { margin: 6px 0; }
  .controls { display: flex; flex-direction: column; gap: 8px; }
  .kv {
    display: flex; align-items: center; gap: 16px; padding: 9px 12px;
    background: var(--card2); border: 1px solid var(--line); border-radius: 9px;
  }
  .key {
    font-family: ui-monospace, SFMono-Regular, monospace; font-weight: 700;
    color: var(--accent); min-width: 96px;
  }
"""


_BRIEFING_TEMPLATE = r"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>__TITLE__</title>
<style>__STYLE__</style>
</head>
<body>
  <div class="wrap">
    <h1>__TITLE__</h1>
    <p class="intro">__INTRO__</p>
    <div id="content">
      __WHATS_NEW__
      __CONTROLS__
      __NOTE__
    </div>
    <div id="launching" class="launching hidden">
      <h2>Launching the game…</h2>
      <p class="intro">Play, then close the game window — a feedback form will pop up here.</p>
    </div>
  </div>
  <div class="bar" id="bar"><button id="startBtn">Start playtest →</button></div>
<script>
document.getElementById('startBtn').addEventListener('click', () => {
  fetch('/submit', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: '{}' })
    .finally(() => {
      document.getElementById('content').classList.add('hidden');
      document.getElementById('bar').classList.add('hidden');
      document.getElementById('launching').classList.remove('hidden');
    });
});
</script>
</body>
</html>
"""


_SURVEY_TEMPLATE = r"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>__TITLE__</title>
<style>__STYLE__</style>
</head>
<body>
  <div class="wrap">
    <h1>__TITLE__</h1>
    <p class="intro">__INTRO__</p>
    <form id="questions" onsubmit="return false;"></form>
    <div id="thanks" class="thanks hidden">
      <h2>Thanks — feedback recorded.</h2>
      <p class="intro">You can close this tab.</p>
    </div>
  </div>
  <div class="bar" id="bar"><button id="submitBtn">Submit feedback</button></div>

<script>
const QUESTIONS = __QUESTIONS_JSON__;
const form = document.getElementById('questions');

function el(tag, cls, txt) {
  const e = document.createElement(tag);
  if (cls) e.className = cls;
  if (txt != null) e.textContent = txt;
  return e;
}

function createQuestion(q, parent) {
  const card = el('div', 'question');
  card.dataset.qid = q.id;
  const label = el('label', 'prompt', q.prompt);
  if (q.required) label.appendChild(el('span', 'req', ' *'));
  card.appendChild(label);

  if (q.type === 'radio' || q.type === 'multiselect') {
    const group = el('div', 'options');
    (q.options || []).forEach(opt => {
      const row = el('label', 'opt');
      const inp = document.createElement('input');
      inp.type = q.type === 'radio' ? 'radio' : 'checkbox';
      inp.name = q.id; inp.value = opt;
      inp.addEventListener('change', () => evalFollowUps(q));
      row.appendChild(inp);
      row.appendChild(el('span', 'optlabel', opt));
      group.appendChild(row);
    });
    card.appendChild(group);
  } else if (q.type === 'textarea') {
    const t = document.createElement('textarea');
    t.name = q.id; t.rows = 3; if (q.placeholder) t.placeholder = q.placeholder;
    card.appendChild(t);
  } else {
    const inp = document.createElement('input');
    inp.type = q.type === 'number' ? 'number' : 'text';
    inp.name = q.id; if (q.placeholder) inp.placeholder = q.placeholder;
    card.appendChild(inp);
  }

  const err = el('div', 'err', 'This one is required.');
  card.appendChild(err);

  if (q.follow_ups && q.follow_ups.length) {
    const wrap = el('div', 'followups');
    q.follow_ups.forEach((fu, i) => {
      const fc = el('div', 'followup hidden');
      fc.dataset.fu = i;
      createQuestion(fu.question, fc);
      wrap.appendChild(fc);
    });
    card.appendChild(wrap);
  }
  parent.appendChild(card);
}

function readValue(q) {
  if (q.type === 'radio') {
    const c = document.querySelector('input[name="' + CSS.escape(q.id) + '"]:checked');
    return c ? c.value : null;
  }
  if (q.type === 'multiselect') {
    return [...document.querySelectorAll('input[name="' + CSS.escape(q.id) + '"]:checked')].map(c => c.value);
  }
  const e = document.querySelector('[name="' + CSS.escape(q.id) + '"]');
  if (!e) return null;
  if (q.type === 'number') return e.value === '' ? null : Number(e.value);
  return e.value;
}

function matches(val, showWhen, type) {
  if (type === 'multiselect') return (val || []).some(v => showWhen.includes(v));
  return showWhen.includes(val);
}

function cardFor(qid) {
  return document.querySelector('.question[data-qid="' + CSS.escape(qid) + '"]');
}

function evalFollowUps(q) {
  if (!q.follow_ups) return;
  const card = cardFor(q.id);
  const val = readValue(q);
  q.follow_ups.forEach((fu, i) => {
    const fc = card.querySelector(':scope > .followups > .followup[data-fu="' + i + '"]');
    if (fc) fc.classList.toggle('hidden', !matches(val, fu.show_when, q.type));
  });
}

function isEmpty(v) { return v == null || v === '' || (Array.isArray(v) && v.length === 0); }

function gather(q, answers, errors) {
  const card = cardFor(q.id);
  const val = readValue(q);
  answers[q.id] = val;
  if (q.required && isEmpty(val)) {
    errors.push(card);
    card.querySelector(':scope > .err').style.display = 'block';
    card.querySelector(':scope > .options, :scope > input, :scope > textarea')?.classList.add('invalid');
  }
  (q.follow_ups || []).forEach((fu, i) => {
    const fc = card.querySelector(':scope > .followups > .followup[data-fu="' + i + '"]');
    if (fc && !fc.classList.contains('hidden')) gather(fu.question, answers, errors);
  });
}

QUESTIONS.forEach(q => createQuestion(q, form));

document.getElementById('submitBtn').addEventListener('click', () => {
  document.querySelectorAll('.err').forEach(e => e.style.display = 'none');
  document.querySelectorAll('.invalid').forEach(e => e.classList.remove('invalid'));
  const answers = {}, errors = [];
  QUESTIONS.forEach(q => gather(q, answers, errors));
  if (errors.length) { errors[0].scrollIntoView({ behavior: 'smooth', block: 'center' }); return; }

  fetch('/submit', {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ answers })
  }).finally(() => {
    form.classList.add('hidden');
    document.getElementById('bar').classList.add('hidden');
    document.getElementById('thanks').classList.remove('hidden');
  });
});
</script>
</body>
</html>
"""
