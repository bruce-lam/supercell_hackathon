import os
import json
import uuid
from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from openai import OpenAI
from pydantic import BaseModel
# ElevenLabs for dramatic genie voice
from elevenlabs.client import ElevenLabs

# --- PATH CONFIGURATION ---
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
STATIC_DIR = os.path.join(BASE_DIR, "static")

# Create static folder if it doesn't exist
if not os.path.exists(STATIC_DIR):
    os.makedirs(STATIC_DIR)

# --- INITIALIZATION ---
app = FastAPI()

# MOUNT STATIC FOLDER
app.mount("/static", StaticFiles(directory=STATIC_DIR), name="static")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# --- CLIENTS ---
# 1. OpenAI for Intelligence (Whisper + GPT-4o)
client = OpenAI(api_key=os.environ.get("OPENAI_API_KEY", "PLACEHOLDER"))

# 2. ElevenLabs for Voice (The Acting)
eleven_client = ElevenLabs(api_key=os.environ.get("ELEVEN_API_KEY", "PLACEHOLDER"))

# Voice ID - pick from ElevenLabs Voice Library
# "Deep Lax" — fun, playful character voice for the Genie
from elevenlabs import VoiceSettings

GENIE_VOICE_ID = os.environ.get("ELEVEN_VOICE_ID", "qZkuFcRFTdS6vkYu5ABx")

# Voice tuning — adjust these for different Genie personalities
GENIE_VOICE_SETTINGS = VoiceSettings(
    stability=0.30,           # Low = more dramatic/erratic (0.0-1.0)
    similarity_boost=0.75,    # High = stays close to original voice
    style=0.40,               # Higher = more theatrical performance
    use_speaker_boost=True    # Enhances voice clarity
)


def generate_eleven_audio(text: str, filepath: str):
    """Generate audio using ElevenLabs and save to file"""
    try:
        audio_stream = eleven_client.text_to_speech.convert(
            voice_id=GENIE_VOICE_ID,
            output_format="mp3_44100_128",
            text=text,
            model_id="eleven_turbo_v2_5",  # Low-latency for games
            voice_settings=GENIE_VOICE_SETTINGS
        )
        with open(filepath, "wb") as f:
            for chunk in audio_stream:
                if chunk:
                    f.write(chunk)
        return True
    except Exception as e:
        print(f"⚠️ ElevenLabs Error: {e}")
        # Fallback to OpenAI TTS if ElevenLabs fails
        try:
            res = client.audio.speech.create(
                model="tts-1", voice="onyx", input=text
            )
            res.stream_to_file(filepath)
            return True
        except Exception as e2:
            print(f"❌ Fallback TTS also failed: {e2}")
            return False

SYSTEM_PROMPT = """
You are a literal-minded, cynical Genie. You HATE opening doors for mortals.
You follow the "Monkey's Paw" philosophy: if a wish is even 1% flawed, the door stays SHUT.

### THE ASSETS (you MUST pick object_name from this list ONLY):
WEAPONS: "sword", "shield", "bomb", "hammer", "potion"
FURNITURE: "chair", "table", "bed", "toilet", "lamp", "door", "crate"
NATURE: "tree", "rock", "mushroom", "flower", "cloud", "fire"
FOOD: "pizza", "burger", "banana", "cheese", "cake"
ANIMALS: "duck", "spider", "fish", "cat"
TOOLS: "key", "ladder", "coin"
SHAPES: "box", "ball"

### THE RIGID DOOR LAWS:
1. DOOR 1 (The Law of Form): Requires LIGHT + PORTABILITY.
2. DOOR 2 (The Law of Substance): Requires MASSIVE WEIGHT + METAL MATERIAL.
3. DOOR 3 (The Law of Purpose): Requires SPECIFIC INTENT.

### TWO-STAGE VOICE INSTRUCTIONS:
1. "drop_voice": A reaction to the physical object as it falls from the pipe. 
   - Example: "Here is your heavy rock. Try not to break your toes."
2. "congrats_voice": The "Verdict" when the player tries the item on the door.
   - If door_open is TRUE: A backhanded, annoyed compliment (e.g., "Fine, it is metal. Get out of my sight.")
   - If door_open is FALSE: A final mocking rejection explaining why they failed (e.g., "Heavy? Yes. Metal? No. The door stays shut.")

OUTPUT FORMAT (JSON ONLY):
{
  "object_name": "string (MUST be from the asset list above)",
  "display_name": "string",
  "hex_color": "#RRGGBB",
  "scale": 0.1 to 5.0,
  "vfx_type": "fire/smoke/sparks/none",
  "door_open": boolean,
  "drop_voice": "Sarcastic insult.",
  "congrats_voice": "Backhanded compliment."
}
"""

@app.post("/process_wish")
async def process_wish(door_id: str = Form(...), file: UploadFile = File(...)):
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
        print(f"User Wish for Door {door_id}: {user_wish}")

        # 2. Get Genie Judgment
        response = client.chat.completions.create(
            model="gpt-4o",
            messages=[
                {"role": "system", "content": SYSTEM_PROMPT},
                {"role": "user", "content": f"Door {door_id}: {user_wish}"}
            ],
            response_format={"type": "json_object"}
        )

        genie_json = json.loads(response.choices[0].message.content)

        # --- 3. DUAL AUDIO GENERATION ---
        audio_id = str(uuid.uuid4())
        drop_file = f"drop_{audio_id}.mp3"
        congrats_file = f"congrats_{audio_id}.mp3"
        
        drop_path = os.path.join(STATIC_DIR, drop_file)
        congrats_path = os.path.join(STATIC_DIR, congrats_file)

        # Generate Audio 1: The Roast (Drop) — ElevenLabs
        print("🎙️ Generating Drop Voice (ElevenLabs)...")
        generate_eleven_audio(genie_json.get("drop_voice", "I refuse."), drop_path)

        # Generate Audio 2: The Verdict (Congrats) — ElevenLabs
        print("🎙️ Generating Verdict Voice (ElevenLabs)...")
        generate_eleven_audio(genie_json.get("congrats_voice", "Fine, you pass."), congrats_path)

        # --- 4. MAP TO TWO DISTINCT URLS IN JSON ---
        genie_json["audio_url_drop"] = f"/static/{drop_file}"
        genie_json["audio_url_congrats"] = f"/static/{congrats_file}"
        
        # Keep the 'active' one as a shortcut
        genie_json["audio_url"] = genie_json["audio_url_congrats"] if genie_json.get("door_open") else genie_json["audio_url_drop"]

        print(f"✅ Created Drop URL: {genie_json['audio_url_drop']}")
        print(f"✅ Created Congrats URL: {genie_json['audio_url_congrats']}")

        return genie_json

    except Exception as e:
        print(f"❌ Error: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))
    finally:
        if os.path.exists(temp_filename):
            os.remove(temp_filename)

# --- INTRO ENDPOINT ---
INTRO_TEXT = (
    "Ah, a new prisoner! Welcome to my chamber, mortal. "
    "You're trapped here until you prove you're not completely useless. "
    "See that door? That's your way out. "
    "But it only opens if you impress me. "
    "Here's how it works: hold the trigger, speak your wish out loud, and I'll decide if you're worthy. "
    "Fair warning — I'm very hard to impress. Good luck... you'll need it."
)

@app.get("/intro")
def get_intro():
    """Generate (and cache) the Genie's intro monologue"""
    intro_path = os.path.join(STATIC_DIR, "intro.mp3")

    # Only generate once — reuse cached file
    if not os.path.exists(intro_path):
        print("🎙️ Generating Genie intro monologue (first time only)...")
        success = generate_eleven_audio(INTRO_TEXT, intro_path)
        if not success:
            raise HTTPException(status_code=500, detail="Failed to generate intro audio")
        print("✅ Intro audio cached at static/intro.mp3")
    else:
        print("📦 Serving cached intro audio")

    return {
        "audio_url": "/static/intro.mp3",
        "subtitle": INTRO_TEXT
    }

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
