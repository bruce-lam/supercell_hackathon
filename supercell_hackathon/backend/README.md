# supercell_hackathon

🧞‍♂️ The Cynical Genie Backend
This is the AI brain behind the Supercell Hackathon project. It uses GPT-4o for logic, Whisper for speech-to-text, and OpenAI TTS for a high-fidelity, sarcastic voice experience.

🚀 Key Features
Dynamic Mystical Laws: The Unity client sends custom "Door Rules" for every request. The Genie adapts its judgment in real-time based on these rules.

Dual-Audio Generation: For every wish, the backend generates two distinct audio files:

Drop Voice: A sarcastic reaction to the physical object falling from the pipe.

Congrats/Verdict Voice: The final judgment delivered when the player interacts with the door.

Monkey’s Paw Logic: The Genie is intentionally difficult. It looks for any technicality to keep the door shut.

Exhaustive Asset Mapping: Maps complex user wishes to a specific set of 3D primitive and low-poly assets (e.g., "A crystal ball" -> sphere).

🛠 Tech Stack
FastAPI: High-performance Python web framework.

OpenAI GPT-4o: Brain for judgment and JSON structuring.

OpenAI Whisper: Converts player voice recordings into text.

OpenAI TTS (Onyx): Provides the "grumpy" voice of the Genie.

🏗 API Specification
POST /process_wish
Processes a voice wish and returns the game logic and audio URLs.

Form Data:
| Key | Type | Description |
| :--- | :--- | :--- |
| door_id | string | The ID of the current door (1, 2, or 3). |
| file | file | The .wav or .mp3 recording of the user's wish. |
| door_rules | string | The specific "laws" the Genie must enforce for this door. |

Example Response:

JSON
{
  "object_name": "crystal_basic",
  "display_name": "glowing diamond",
  "hex_color": "#00FFFF",
  "scale": 1.2,
  "vfx_type": "sparks",
  "door_open": true,
  "drop_voice": "A diamond? How original. Try not to lose it.",
  "congrats_voice": "It matches the laws of the ancient door. Proceed.",
  "audio_url_drop": "/static/drop_uuid.mp3",
  "audio_url_congrats": "/static/congrats_uuid.mp3"
}
🏃‍♂️ Setup & Installation
Install Dependencies:

Bash
pip install -r requirements.txt
Run the Server:

Bash
python3 main.py
Unity Connection:
Point your Unity UnityWebRequest to http://YOUR_IP:8000/process_wish.

🧪 Quick Test Command
You can test the backend without Unity using this command:

Bash
curl -X POST http://localhost:8000/process_wish \
  -F "door_id=1" \
  -F "file=@test.wav" \
  -F "door_rules=The door only opens for small purple fruit."
