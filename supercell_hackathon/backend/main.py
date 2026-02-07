from fastapi import FastAPI, UploadFile, File
from fastapi.staticfiles import StaticFiles
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
import os
import shutil
import uuid
import json
import requests
from openai import OpenAI

# ==========================================
# 🔑 API KEYS
# ==========================================
OPENAI_API_KEY = "PLACEHOLDER"
REACTOR_API_KEY = "PLACEHOLDER"

# ==========================================
# ⚙️ SETUP
# ==========================================
app = FastAPI()

# Allow Unity to connect
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# Audio file hosting
os.makedirs("static", exist_ok=True)
app.mount("/static", StaticFiles(directory="static"), name="static")

# OpenAI Client
client = OpenAI(api_key=OPENAI_API_KEY)

# 🧠 MEMORY: Keep track of what the user collected
session_history = [] 

# ==========================================
# 🌀 REACTOR FUNCTION (The Portal)
# ==========================================
def generate_reactor_video(prompt):
    print(f"🌀 Reactor Generating: {prompt}")
    
    url = "https://api.reactor.inc/v1/livecore/generate"
    headers = {
        "Authorization": f"Bearer {REACTOR_API_KEY}",
        "Content-Type": "application/json"
    }
    payload = {
        "prompt": prompt,
        "duration": 10,
        "fps": 24
    }
    
    try:
        response = requests.post(url, json=payload, headers=headers)
        
        # 🔍 DEBUGGING: Print exactly what they sent back
        print(f"📡 Reactor Status: {response.status_code}")
        print(f"📦 Reactor Raw Response: '{response.text}'")

        # If it's just a URL string (not JSON), handle it!
        if response.text.startswith("http"):
            return response.text.strip().replace('"', '')

        data = response.json()
        return data.get("stream_url") or data.get("video_url")

    except Exception as e:
        print(f"❌ Reactor Error: {e}")
        return "https://media.giphy.com/media/l0MYt5jPR6QX5pnqM/giphy.mp4"
# ==========================================
# 🧠 THE BRAIN (OpenAI)
# ==========================================
SYSTEM_PROMPT = """
You are a mischievous Genie. 
The user makes a wish. You grant it with a chaotic twist.
Output JSON only.

Format:
{
  "object_name": "exact_unity_prefab_name",
  "twist": "funny_explanation",
  "voice_response": "what_you_say_out_loud"
}

Allowed Objects: sword, shield, bomb, pizza, duck, chair, table
"""

async def run_genie_brain(user_text):
    print(f"🧠 Thinking: {user_text}")
    
    # 1. Ask GPT-4o
    completion = client.chat.completions.create(
        model="gpt-4o-mini",
        messages=[
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": user_text}
        ],
        response_format={"type": "json_object"}
    )
    
    ai_data = json.loads(completion.choices[0].message.content)
    
    # 2. Generate Voice (TTS)
    voice_filename = f"voice_{uuid.uuid4()}.mp3"
    voice_path = os.path.join("static", voice_filename)
    
    client.audio.speech.create(
        model="tts-1", voice="onyx", input=ai_data["voice_response"]
    ).stream_to_file(voice_path)

    # 3. Add Audio URL
    # 🔴 REPLACE WITH YOUR IP ADDRESS
    my_ip = "10.30.136.183"
    ai_data["audio_url"] = f"http://{my_ip}:8000/static/{voice_filename}"
    
    # 4. Save to Memory (For the Finale)
    session_history.append(ai_data["object_name"])
    
    return ai_data

# ==========================================
# 🚀 ENDPOINTS
# ==========================================

# 1️⃣ HEAR WISH (Game Loop)
@app.post("/hear")
async def hear_wish(file: UploadFile = File(...)):
    print("👂 Receiving Audio...")

    # Save & Transcribe
    temp_filename = f"temp_{uuid.uuid4()}.wav"
    with open(temp_filename, "wb") as buffer:
        shutil.copyfileobj(file.file, buffer)
    
    with open(temp_filename, "rb") as audio:
        transcription = client.audio.transcriptions.create(
            model="whisper-1", file=audio
        )
    os.remove(temp_filename)

    # Run Brain
    return await run_genie_brain(transcription.text)

# 2️⃣ WAKE UP (The Finale)
@app.post("/wakeup")
async def trigger_wakeup():
    print("⏰ Waking Up...")

    # 1. Generate 'Real World' Video
    prompt = "POV waking up in a messy bedroom, morning sunlight, unmade bed, 4k"
    video_url = generate_reactor_video(prompt)
    
    # 2. Voice Line
    voice_text = "Wait... if I'm awake... why is all this stuff on my bed?"
    voice_filename = f"wakeup_{uuid.uuid4()}.mp3"
    client.audio.speech.create(
        model="tts-1", voice="onyx", input=voice_text
    ).stream_to_file(os.path.join("static", voice_filename))

    my_ip = "10.30.136.183"

    return {
        "action": "wakeup_sequence",
        "background_video": video_url,      # 🟢 Apply to Sphere
        "objects_collected": session_history, # 🟢 Spawn these on Bed
        "audio_url": f"http://{my_ip}:8000/static/{voice_filename}"
    }
