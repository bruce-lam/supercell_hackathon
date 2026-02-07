import os
import json
import uuid
from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from openai import OpenAI
from pydantic import BaseModel
# --- ELEVENLABS IMPORT ---
from elevenlabs.client import ElevenLabs

# --- PATH CONFIGURATION ---
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
STATIC_DIR = os.path.join(BASE_DIR, "static")

if not os.path.exists(STATIC_DIR):
    os.makedirs(STATIC_DIR)

# --- INITIALIZATION ---
app = FastAPI()
app.mount("/static", StaticFiles(directory=STATIC_DIR), name="static")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# --- CLIENTS ---
# Your Keys
client = OpenAI(api_key="")
eleven_client = ElevenLabs(api_key="")

# Voice ID: "pNInz6obpgDQGcFmaJgB" (Adam) is deep and cynical
GENIE_VOICE_ID = "pNInz6obpgDQGcFmaJgB"

# --- ELEVENLABS GENERATOR ---
def generate_eleven_audio(text: str, filepath: str):
    """Generate audio using ElevenLabs with OpenAI fallback"""
    try:
        audio_stream = eleven_client.text_to_speech.convert(
            voice_id=GENIE_VOICE_ID,
            output_format="mp3_44100_128",
            text=text,
            model_id="eleven_turbo_v2_5"
        )
        with open(filepath, "wb") as f:
            for chunk in audio_stream:
                if chunk: f.write(chunk)
        return True
    except Exception as e:
        print(f"⚠️ ElevenLabs Error: {e}")
        try:
            res = client.audio.speech.create(model="tts-1", voice="onyx", input=text)
            res.stream_to_file(filepath)
            return True
        except Exception as e2:
            print(f"❌ Fallback failed: {e2}")
            return False

SYSTEM_PROMPT = """
You are a literal-minded, cynical Genie. You HATE opening doors for mortals.
STRICT RULE: You will be provided with the 'CURRENT DOOR LAWS' in the user message. 
You MUST judge the wish based ONLY on those provided laws.

### ACTING INSTRUCTIONS:
Use ellipses (...) for dramatic pauses, capitalize words for emphasis, and use expressive punctuation. 
Your tone is bored, gravelly, and unimpressed.

### THE ASSETS:
- "sphere", "cube", "cylinder", "capsule", "anvil_basic", "stick_basic", 
- "key_basic", "duck_basic", "blade_basic", "shield_basic", "crystal_basic", 
- "chalice_basic", "book_basic", "ring_basic", "cone_basic", "heart_basic", "star_basic"

### TWO-STAGE VOICE INSTRUCTIONS:
1. "drop_voice": A reaction to the physical object as it falls from the pipe. 
2. "congrats_voice": The "Verdict" when the player tries the item on the door.

OUTPUT FORMAT (JSON ONLY):
{
  "object_name": "string",
  "display_name": "string",
  "hex_color": "#RRGGBB",
  "scale": 0.1 to 5.0,
  "vfx_type": "fire/smoke/sparks/none",
  "door_open": boolean,
  "drop_voice": "Sarcastic reaction to the item... e.g., 'Oh look... a BOX. How... riveting.'",
  "congrats_voice": "Backhanded verdict... e.g., 'It fits the law... technically. I suppose you may pass.'"
}
"""
@app.get("/get_rules")
async def get_rules():
    """Generates 3 Laws and 3 Mystical Clues for the player."""
    try:
        response = client.chat.completions.create(
            model="gpt-4o",
            messages=[
                {"role": "system", "content": """
                You are a cynical Genie game designer. Generate 3 unique laws for 3 doors.
                
                FORMAT: Return a JSON object with a list called 'doors'.
                Each door must have:
                1. 'law': A strict physical requirement (e.g., 'Must be made of gold').
                2. 'clue': A cryptic, cynical hint for the player (e.g., 'Only that which glitters like the sun's greed shall pass').
                
                GUIDELINES:
                - Laws must be physical (color, material, weight, or shape).
                - Clues should be atmospheric and 'Genie-like' but helpful.
                """},
                {"role": "user", "content": "Generate 3 laws and clues."}
            ],
            response_format={"type": "json_object"}
        )
        return json.loads(response.choices[0].message.content)
    except Exception as e:
        print(f"❌ Error: {e}")
        # Robust Fallback
        return {
            "doors": [
                {"law": "Must be red", "clue": "Bring me the color of a fresh wound... or a strawberry."},
                {"law": "Must be metal", "clue": "Only the cold, unfeeling iron of the earth opens this path."},
                {"law": "Must be round", "clue": "I seek an object with no beginning and no end."}
            ]
        }

@app.post("/process_wish")
async def process_wish(
    door_id: str = Form(...), 
    file: UploadFile = File(...),
    door_rules: str = Form(...) 
):
    temp_filename = os.path.join(BASE_DIR, f"temp_{uuid.uuid4()}.wav")
    
    with open(temp_filename, "wb") as buffer:
        buffer.write(await file.read())

    try:
        # 1. Transcribe the audio
        with open(temp_filename, "rb") as audio_file:
            transcription = client.audio.transcriptions.create(
                model="whisper-1", 
                file=audio_file
            )
        
        user_wish = transcription.text
        print(f"Wish: {user_wish}")

        # 2. Get Genie Judgment
        response = client.chat.completions.create(
            model="gpt-4o",
            messages=[
                {"role": "system", "content": SYSTEM_PROMPT},
                {"role": "user", "content": f"THE CURRENT DOOR LAWS:\n{door_rules}\n\nUser Wish: {user_wish}"}
            ],
            response_format={"type": "json_object"}
        )

        genie_json = json.loads(response.choices[0].message.content)

        # --- 3. DUAL AUDIO GENERATION (ElevenLabs) ---
        audio_id = str(uuid.uuid4())
        drop_file = f"drop_{audio_id}.mp3"
        congrats_file = f"congrats_{audio_id}.mp3"
        drop_path = os.path.join(STATIC_DIR, drop_file)
        congrats_path = os.path.join(STATIC_DIR, congrats_file)

        generate_eleven_audio(genie_json.get("drop_voice", "I refuse."), drop_path)
        generate_eleven_audio(genie_json.get("congrats_voice", "Fine, you pass."), congrats_path)

        # --- 4. MAP TO URLS ---
        genie_json["audio_url_drop"] = f"/static/{drop_file}"
        genie_json["audio_url_congrats"] = f"/static/{congrats_file}"
        return genie_json

    except Exception as e:
        print(f"❌ Error: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))
    finally:
        if os.path.exists(temp_filename):
            os.remove(temp_filename)

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
