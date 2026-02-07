import os
import json
import uuid
from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from openai import OpenAI
from pydantic import BaseModel

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

# Your Key
client = OpenAI(api_key="PLACEHOLDER")

SYSTEM_PROMPT = """
You are a literal-minded, cynical Genie. You HATE opening doors for mortals.
You follow the "Monkey's Paw" philosophy: if a wish is even 1% flawed, the door stays SHUT.

### THE ASSETS:
- "sphere", "cube", "cylinder", "capsule", "anvil_basic", "stick_basic", 
- "key_basic", "duck_basic", "blade_basic", "shield_basic", "crystal_basic", 
- "chalice_basic", "book_basic", "ring_basic", "cone_basic", "heart_basic", "star_basic"

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
  "object_name": "string",
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

        # Generate Audio 1: The Roast (Drop)
        res_drop = client.audio.speech.create(
            model="tts-1",
            voice="onyx",
            input=genie_json.get("drop_voice", "I refuse.")
        )
        res_drop.stream_to_file(drop_path)

        # Generate Audio 2: The Praise (Congrats)
        res_congrats = client.audio.speech.create(
            model="tts-1",
            voice="onyx",
            input=genie_json.get("congrats_voice", "Fine, you pass.")
        )
        res_congrats.stream_to_file(congrats_path)

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

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
