"""Dev helper: render the survey with sample questions (all field types) to a
temp HTML file for screenshot testing. Run: `python3 preview.py`."""

import os
import tempfile

from survey import render_briefing, render_survey

SAMPLE = [
    {
        "id": "overall",
        "type": "radio",
        "prompt": "Overall, did the build feel good to move around in?",
        "options": ["Yes", "Somewhat", "No"],
        "required": True,
    },
    {
        "id": "dodge_iframes",
        "type": "radio",
        "prompt": "Did dodging grant invincibility when you expected it to?",
        "options": ["Yes", "Too early", "Too late", "No invuln at all"],
        "required": True,
        "follow_ups": [
            {
                "show_when": ["Too early", "Too late", "No invuln at all"],
                "question": {
                    "id": "dodge_detail",
                    "type": "textarea",
                    "prompt": "Describe what you experienced instead:",
                    "placeholder": "e.g. I got clipped halfway through the roll...",
                    "required": True,
                },
            }
        ],
    },
    {
        "id": "verbs_used",
        "type": "multiselect",
        "prompt": "Which verbs did you actually use? (check all)",
        "options": ["Run", "Kick", "Harpoon grapple"],
    },
    {
        "id": "camera_offs",
        "type": "number",
        "prompt": "Roughly how many times did the camera feel wrong?",
        "placeholder": "0",
    },
    {
        "id": "harpoon_word",
        "type": "text",
        "prompt": "One word for how the harpoon pull felt:",
        "placeholder": "snappy / floaty / sluggish ...",
    },
    {
        "id": "anything",
        "type": "textarea",
        "prompt": "Anything else worth noting?",
        "placeholder": "Optional",
    },
]


BRIEFING = {
    "whats_new": [
        "High-seas archipelago with grapple spires",
        "Kick attack",
        "Harpoon pull-self traversal",
        "Cel / edge outline look",
    ],
    "controls": [
        {"keys": "WASD", "action": "Move"},
        {"keys": "J", "action": "Kick"},
        {"keys": "Space", "action": "Fire harpoon at a glowing spire to grapple"},
    ],
    "note": "Close the game window when you're done — a feedback form will pop up here.",
}


def main():
    survey_out = os.path.join(tempfile.gettempdir(), "playtest_preview.html")
    with open(survey_out, "w", encoding="utf-8") as f:
        f.write(render_survey(SAMPLE, title="Crimson Corsair — playtest",
                              intro="You just played the high-seas slice. A few quick questions:"))
    briefing_out = os.path.join(tempfile.gettempdir(), "playtest_briefing.html")
    with open(briefing_out, "w", encoding="utf-8") as f:
        f.write(render_briefing(BRIEFING))
    print(survey_out)
    print(briefing_out)


if __name__ == "__main__":
    main()
