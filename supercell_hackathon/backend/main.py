from fastapi import FastAPI, UploadFile, File
from fastapi.staticfiles import StaticFiles
from fastapi.middleware.cors import CORSMiddleware
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
# ⚙️ SETUP & GAME STATE
# ==========================================
app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# Host audio files for Unity to play
os.makedirs("static", exist_ok=True)
app.mount("/static", StaticFiles(directory="static"), name="static")

client = OpenAI(api_key=OPENAI_API_KEY)

# 🎮 GLOBAL GAME STATE
# current_door: 1 (Light), 2 (Weight), 3 (Sound)
game_state = {"current_door": 1} 
session_history = [] 

# ==========================================
# 🌀 REACTOR (The Reality Reveal)
# ==========================================
def generate_reactor_video(prompt):
    print(f"🌀 Reactor Generating: {prompt}")
    url = "https://api.reactor.inc/v1/livecore/generate"
    headers = {
        "Authorization": f"Bearer {REACTOR_API_KEY}",
        "X-Reactor-Invite": "REALTIMEVIDEO",
        "Content-Type": "application/json"
    }
    payload = {"prompt": prompt, "duration": 5, "fps": 24}
    
    try:
        response = requests.post(url, json=payload, headers=headers)
        if response.status_code == 200:
            data = response.json()
            return data.get("video_url") or data.get("stream_url")
    except Exception as e:
        print(f"❌ Reactor Error: {e}")
    
    # Fallback video if API fails during demo
    return "https://joy1.videvo.net/videvo_files/video/free/2019-11/large_watermarked/190301_1_25_11_preview.mp4"

# ==========================================
# 🧠 THE BRAIN (OpenAI GPT-4o-mini)
# ==========================================
SYSTEM_PROMPT = """
You are a sarcastic Game Master Genie. The user is trapped in a dream hallway with 3 doors.
To open a door, the user must wish for an object that satisfies the door's requirement.

THE CHALLENGES:
- Door 1 Requirement: Needs LIGHT (e.g., lantern, glowing pizza, sun, lightsaber).
- Door 2 Requirement: Needs WEIGHT (e.g., anvil, massive rock, elephant, neutron star).
- Door 3 Requirement: Needs MUSIC/SOUND (e.g., flute, screaming goat, radio, boombox).

YOUR EVALUATION:
1. If the wish reasonably satisfies the current requirement, set "door_open" to true.
2. SUCCESS RESPONSE: "Congratulations... [sarcastic/insulting comment about how they used that specific object]. Move to the next door."
3. FAILURE RESPONSE: Mock them for their useless choice. Set "door_open" to false.

Output JSON ONLY:
{
  "object_name": "unity_prefab_name",
  "twist": "funny_explanation",
  "voice_response": "your_sarcastic_commentary",
  "door_open": true_or_false
}
"""

async def run_genie_brain(user_text):
    door_num = game_state["current_door"]
    print(f"🧠 Processing wish for Door {door_num}: {user_text}")
    
    completion = client.chat.completions.create(
        model="gpt-4o-mini",
        messages=[
            {"role": "system", "content": f"{SYSTEM_PROMPT}\nCURRENT OBJECTIVE: Door {door_num}"},
            {"role": "user", "content": user_text}
        ],
        response_format={"type": "json_object"}
    )
    
    ai_data = json.loads(completion.choices[0].message.content)
    
    # 🔓 Logic: If they pass, increment the door
    if ai_data.get("door_open") is True:
        if game_state["current_door"] < 3:
            game_state["current_door"] += 1
        else:
            ai_data["voice_response"] += " That was the final door. Reality is calling. Wake up."

    # Generate Voice (TTS)
    voice_filename = f"voice_{uuid.uuid4()}.mp3"
    voice_path = os.path.join("static", voice_filename)
    client.audio.speech.create(
        model="tts-1", voice="onyx", input=ai_data["voice_response"]
    ).stream_to_file(voice_path)

    # 🔴 REPLACE WITH YOUR LOCAL IP (10.30.136.183)
    my_ip = "10.30.136.183" 
    ai_data["audio_url"] = f"http://{my_ip}:8000/static/{voice_filename}"
    ai_data["active_door"] = door_num 
    
    session_history.append(ai_data["object_name"])
    return ai_data

# ==========================================
# 🚀 ENDPOINTS
# ==========================================

@app.post("/hear")
async def hear_wish(file: UploadFile = File(...)):
    print("👂 Receiving Audio...")
    temp_filename = f"temp_{uuid.uuid4()}.wav"
    with open(temp_filename, "wb") as buffer:
        shutil.copyfileobj(file.file, buffer)
    
    with open(temp_filename, "rb") as audio:
        transcription = client.audio.transcriptions.create(
            model="whisper-1", file=audio
        )
    os.remove(temp_filename)

    return await run_genie_brain(transcription.text)

@app.post("/wakeup")
async def trigger_wakeup():
    print("⏰ Waking Up...")
    # Generate cinematic bedroom reveal
    prompt = "POV waking up in a messy bedroom, morning sunlight, cinematic lighting, 4k"
    video_url = generate_reactor_video(prompt)
    
    # Finale Voice
    voice_text = "Wait... if I'm awake... why are all these things still here?"
    voice_filename = f"wakeup_{uuid.uuid4()}.mp3"
    client.audio.speech.create(
        model="tts-1", voice="onyx", input=voice_text
    ).stream_to_file(os.path.join("static", voice_filename))

    my_ip = "10.30.136.183"

    return {
        "action": "wakeup_sequence",
        "background_video": video_url,
        "objects_collected": session_history,
        "audio_url": f"http://{my_ip}:8000/static/{voice_filename}"
    }

@app.post("/reset")
async def reset_game():
    game_state["current_door"] = 1
    session_history.clear()
    return {"message": "Game reset to Door 1"}
