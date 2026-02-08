🧞‍♂️ The Cynical Genie Backend: ElevenLabs Edition
This is the AI brain behind the Supercell Hackathon project. It handles voice processing, cynical judgment logic, and high-fidelity vocal performances.

🚀 Key Features
Dual-Engine TTS (ElevenLabs + OpenAI): * Primary: Uses ElevenLabs (Adam) for a deep, gravelly, and dramatic cinematic performance.

Fallback: Automatically switches to OpenAI Onyx if ElevenLabs API limits are reached, ensuring zero downtime during demos.

Dynamic Mystical Laws: The Unity client sends custom "Door Rules" per request. The Genie adapts its judgment in real-time based on the specific "laws" of that run.

Two-Stage Narrative Audio: 1.  Drop Voice: A sarcastic reaction triggered when the object is spawned.
2.  Verdict Voice: The final judgment (praise or rejection) played when the player hits the door.

Exhaustive Asset Mapping: Maps natural language wishes to a specific library of 3D assets (e.g., "A shiny ruby" -> crystal_basic).

🛠 Tech Stack
FastAPI: High-performance Python web framework.

OpenAI GPT-4o: Brain for judgment and "Monkey's Paw" logic.

OpenAI Whisper: Converts player voice recordings into text.

ElevenLabs (Turbo v2.5): Cinematic voice acting with low-latency streaming.

🏗 API Specification
POST /process_wish
Processes a voice wish and returns game logic + dual audio URLs.

Form Data:
| Key | Type | Description |
| :--- | :--- | :--- |
| door_id | string | ID of the current door (1, 2, or 3). |
| file | file | .wav or .mp3 recording of the user's wish. |
| door_rules | string | The specific "laws" the Genie must enforce (e.g., "Must be metal"). |

Example Response:

JSON
{
  "object_name": "anvil_basic",
  "display_name": "Heavy Iron Block",
  "door_open": true,
  "drop_voice": "An iron block... how heavy... and how boring.",
  "congrats_voice": "It is metal, and it is heavy. I suppose you may enter... for now.",
  "audio_url_drop": "/static/drop_uuid.mp3",
  "audio_url_congrats": "/static/congrats_uuid.mp3"
}
🏃‍♂️ Setup & Installation
Install Dependencies:

Bash
pip install fastapi uvicorn openai elevenlabs python-multipart
Environment Variables:
Ensure your OPENAI_API_KEY and ELEVEN_API_KEY are set in main.py.

Run the Server:

Bash
python3 main.py
🧪 Quick Test Command (Generate & Send)
Test the full pipeline (Transcription -> GPT -> ElevenLabs) with one command:

Bash
say "I want a tiny purple grape" --data-format=LEI16@44100 -o test.wav && \
curl -X POST http://localhost:8000/process_wish \
  -F "door_id=1" \
  -F "file=@test.wav" \
  -F "door_rules=The door only opens for small purple fruit."
