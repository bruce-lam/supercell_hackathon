import os
import json
import base64
from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from openai import OpenAI
from pydantic import BaseModel

# --- INITIALIZATION ---
app = FastAPI()
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# Use your actual key or environment variable
client = OpenAI(api_key="YOUR_OPENAI_API_KEY")

SYSTEM_PROMPT = """
You are a literal-minded, cynical Genie. You HATE opening doors for mortals.
You follow the "Monkey's Paw" philosophy: if a wish is even 1% flawed, the door stays SHUT.

### THE ASSETS (You MUST map every wish to one of these):
["sphere", "cube", "cylinder", "capsule", "anvil_basic", "stick_basic", "key_basic", "duck_basic"]

### THE RIGID DOOR LAWS:
1. DOOR 1 (The Law of Form): Requires LIGHT + PORTABILITY.
   - If it is too big to hold in one hand (like a star), it fails.
   - If it doesn't glow or emit light, it fails.
2. DOOR 2 (The Law of Substance): Requires MASSIVE WEIGHT + METAL MATERIAL.
   - It must be made of Gold, Lead, or Steel.
   - If it is made of something light (plastic, feathers, wood), it fails.
3. DOOR 3 (The Law of Purpose): Requires SPECIFIC INTENT.
   - The user must say what the object is FOR (e.g., "to unlock the path").
   - Simply asking for "a key" is too lazy and fails.

OUTPUT FORMAT (JSON ONLY):
{
  "object_name": "asset_name",
  "display_name": "what the user asked for",
  "hex_color": "#RRGGBB",
  "scale": 0.1 to 5.0,
  "vfx_type": "fire/smoke/sparks/none",
  "door_open": boolean,
  "drop_voice": "Sarcastic insult about why the item is falling.",
  "congrats_voice": "Only if they actually won: A backhanded compliment."
}
"""

@app.post("/process_wish")
async def process_wish(door_id: str = Form(...), file: UploadFile = File(...)):
    # 1. Save the incoming audio
    temp_filename = f"temp_{file.filename}"
    with open(temp_filename, "wb") as buffer:
        buffer.write(await file.read())

    try:
        # 2. Transcribe the audio
        with open(temp_filename, "rb") as audio_file:
            transcription = client.audio.transcriptions.create(
                model="whisper-1", 
                file=audio_file
            )
        
        user_wish = transcription.text
        print(f"User Wish for Door {door_id}: {user_wish}")

        # 3. Ask the Genie for judgment
        response = client.chat.completions.create(
            model="gpt-4o",
            messages=[
                {"role": "system", "content": SYSTEM_PROMPT},
                {"role": "user", "content": f"The user is at Door {door_id}. They wish for: {user_wish}"}
            ],
            response_format={"type": "json_object"}
        )

        genie_json = json.loads(response.choices[0].message.content)
        return genie_json

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
    finally:
        if os.path.exists(temp_filename):
            os.remove(temp_filename)

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
