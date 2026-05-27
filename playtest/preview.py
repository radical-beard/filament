"""Dev helper: render the survey with sample questions (all field types) to a
temp HTML file for screenshot testing. Run: `python3 preview.py`."""

import os
import tempfile

from survey import render_survey

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


def main():
    page = render_survey(
        SAMPLE,
        title="Crimson Corsair — playtest",
        intro="You just played the high-seas slice. A few quick questions:",
    )
    out = os.path.join(tempfile.gettempdir(), "playtest_preview.html")
    with open(out, "w") as f:
        f.write(page)
    print(out)


if __name__ == "__main__":
    main()
