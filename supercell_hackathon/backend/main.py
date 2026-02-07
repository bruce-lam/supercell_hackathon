from fastapi import FastAPI
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel
from openai import OpenAI
import os
import json
import uuid

app = FastAPI()

app.mount("/static", StaticFiles(directory="static"), name="static")

# ==========================================
# PASTE YOUR OPENAI KEY HERE
# ==========================================
OPENAI_API_KEY = "INSER_KEY_HERE"

client = OpenAI(api_key=OPENAI_API_KEY)

# 🔴 THE MASSIVE ASSET LIBRARY
SYSTEM_PROMPT = """
You are the 'Dreamkeeper', an omnipotent and chaotic AI Genie.
The user will wish for something, and you must grant it using ONLY the assets below.

AVAILABLE ASSETS (You must output the 'code_name' exactly):
- WEAPONS: "sword", "shield", "bomb", "hammer", "potion"
- FURNITURE: "chair", "table", "bed", "toilet", "lamp", "door", "chest"
- NATURE: "tree", "rock", "mushroom", "flower", "cloud", "fire"
- FOOD: "pizza", "burger", "banana", "cheese", "cake"
- ANIMALS: "duck", "spider", "fish", "cat"
- TOOLS: "key", "ladder", "coin"
- SHAPES: "box", "ball"

AVAILABLE TWISTS:
- "GIANT" (Building sized)
- "TINY" (Bug sized)
- "CEILING" (Upside down)
- "BOUNCY" (Physics madness)
- "SPINNING" (Rotates constantly)
- "NONE"

RULES:
1. INTERPRET THE WISH CREATIVELY.
   - User: "I want to fly." -> Asset: "cloud", Twist: "BOUNCY"
   - User: "I'm hungry." -> Asset: "pizza", Twist: "GIANT"
   - User: "Protect me!" -> Asset: "shield", Twist: "SPINNING"
   - User: "I want a friend." -> Asset: "duck", Twist: "NONE"
   
2. VOICE RESPONSE:
   - Be sarcastic, wise, or chaotic.
   - Explain *why* you chose that object.
   - Example: "You want flight? Here is a bouncy cloud. Try not to fall off."

3. OUTPUT JSON ONLY.
"""

class Wish(BaseModel):
    text: str

@app.post("/wish")
async def process_wish(wish: Wish):
    print(f"🧠 Brain thinking about: {wish.text}")
    
    completion = client.chat.completions.create(
        model="gpt-4o-mini",
        messages=[
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": wish.text}
        ],
        response_format={"type": "json_object"}
    )
    
    raw_content = completion.choices[0].message.content
    print(f"🤖 RAW AI REPLY: {raw_content}") 
    ai_data = json.loads(raw_content)
    
    # 🔴 FIX: Check for "voice_response" OR "response" OR "voice"
    voice_text = ai_data.get("voice_response") or \
                 ai_data.get("response") or \
                 ai_data.get("voice") or \
                 "Granted."
    
    print(f"🗣️ Speaking: {voice_text}")

    speech_file_name = f"{uuid.uuid4()}.mp3"
    speech_file_path = os.path.join("static", speech_file_name)
    
    response = client.audio.speech.create(
        model="tts-1",
        voice="onyx",
        input=voice_text
    )
    response.stream_to_file(speech_file_path)

    # 🔴 MAKE SURE THIS IP MATCHES YOUR COMPUTER (10.30.136.183)
    my_ip = "10.30.136.183" 
    ai_data["audio_url"] = f"http://10.30.136.183:8000/static/{speech_file_name}"
    
    # Ensure the Unity script gets the right key too
    ai_data["voice_response"] = voice_text
    
    return ai_data
