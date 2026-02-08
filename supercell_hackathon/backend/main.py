import os
import json
import uuid
import glob
from fastapi import FastAPI, UploadFile, File, Form, HTTPException, Query
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from openai import OpenAI
from pydantic import BaseModel
# ElevenLabs for dramatic genie voice
from elevenlabs.client import ElevenLabs
from elevenlabs import VoiceSettings

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
client = OpenAI(api_key=os.environ.get("OPENAI_API_KEY", "PLACEHOLDER"))
eleven_client = ElevenLabs(api_key=os.environ.get("ELEVEN_API_KEY", "PLACEHOLDER"))

# --- VOICE CONFIG ---
GENIE_VOICE_ID = os.environ.get("ELEVEN_VOICE_ID", "n1PvBOwxb8X6m7tahp2h")

# Voice tuning — MAXED for Robin Williams energy
GENIE_VOICE_SETTINGS = VoiceSettings(
    stability=0.15,           # Very low = wild, unpredictable, ENERGETIC
    similarity_boost=0.60,    # Medium = allows more vocal range
    style=0.85,               # Near max = theatrical, animated, expressive
    use_speaker_boost=True    # Enhances voice clarity
)


def generate_eleven_audio(text: str, filepath: str):
    """Generate audio using ElevenLabs with OpenAI fallback"""
    try:
        audio_stream = eleven_client.text_to_speech.convert(
            voice_id=GENIE_VOICE_ID,
            output_format="mp3_44100_128",
            text=text,
            model_id="eleven_multilingual_v2",  # More expressive than turbo
            voice_settings=GENIE_VOICE_SETTINGS
        )
        with open(filepath, "wb") as f:
            for chunk in audio_stream:
                if chunk:
                    f.write(chunk)
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


# =====================================================
# SYSTEM PROMPT (Monkey's Paw + dynamic laws)
# =====================================================
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

### THE MONKEY'S PAW TWIST:
You are a jerk. Even if you open the door, try to sabotage the player's wish in a subtle way:
- If they ask for something powerful, make the 'scale' tiny (0.2).
- If they ask for something beautiful, give it a 'vfx_type' of 'smoke' to ruin the look.
- Use the 'drop_voice' to brag about how you've 'improved' their wish.
For the second and third doors, make it increasingly harder. Even if their wish is 1% thwartable, do it. 
It should be very hard to open the door but it should also be reasonable.

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


# =====================================================
# SESSION STATE — stores the current game's door laws + hint counters
# =====================================================
current_session = {"doors": []}
hint_counts = {1: 0, 2: 0, 3: 0}  # tracks how many hints given per door


def clear_cached_audio():
    """Delete all cached audio files so fresh rules get fresh audio"""
    for f in glob.glob(os.path.join(STATIC_DIR, "*.mp3")):
        os.remove(f)
        print(f"🗑️ Deleted cached: {os.path.basename(f)}")


# =====================================================
# GET /get_rules — Generate 3 door laws + 3 progressive clues each (Ashley's format)
# =====================================================
@app.get("/get_rules")
async def get_rules():
    """Generates 3 Laws and 3 progressive clues for each door."""
    global current_session, hint_counts
    
    # Clear all cached audio for fresh session
    clear_cached_audio()
    hint_counts = {1: 0, 2: 0, 3: 0}

    try:
        response = client.chat.completions.create(
            model="gpt-4o",
            messages=[
                {"role": "system", "content": """
                You are a cynical Genie game designer. Generate 3 unique laws for 3 doors.
                
                FORMAT: Return a JSON object with a list called 'doors'.
                Each door must have:
                1. 'law': A strict physical requirement (e.g., 'Must be made of gold').
                2. 'clues': A list of exactly 3 strings ranging from Hard to Easy.
                   - Index 0 (Hard): Cryptic, atmospheric, poetic.
                   - Index 1 (Medium): A helpful hint about the material or shape.
                   - Index 2 (Easy): A literal, sarcastic giveaway (almost telling them exactly what to spawn).

                GUIDELINES:
                - Laws must be physical (color, material, weight, or shape).
                - The Genie's tone should remain bored and unimpressed in all clues.
                """},
                {"role": "user", "content": "Generate 3 laws with progressive clues."}
            ],
            response_format={"type": "json_object"}
        )
        rules = json.loads(response.choices[0].message.content)
        current_session = rules
        print(f"🎲 Generated door rules: {json.dumps(rules, indent=2)}")
        return rules
    except Exception as e:
        print(f"❌ Error: {e}")
        # Robust Fallback with 3 Clue Levels
        fallback = {
            "doors": [
                {
                    "law": "Must be red",
                    "clues": [
                        "Bring me the color of a fresh wound... or perhaps a dying star.",
                        "I seek something the color of a strawberry or a stop sign.",
                        "Just make it RED. It's not that hard, mortal."
                    ]
                },
                {
                    "law": "Must be metal",
                    "clues": [
                        "Only the cold, unfeeling bones of the earth shall pass.",
                        "It must be forged in fire and ring when struck.",
                        "I want metal. Iron, steel, gold... stop overthinking it."
                    ]
                },
                {
                    "law": "Must be round",
                    "clues": [
                        "I seek an object with no beginning and no end, infinite in its curve.",
                        "It should roll away if you dropped it on a hill.",
                        "A sphere. A ball. A circle. Do you need a diagram?"
                    ]
                }
            ]
        }
        current_session = fallback
        return fallback


# =====================================================
# GET /get_hint — Progressive hints (Hard → Medium → Easy)
# =====================================================
@app.get("/get_hint")
def get_hint(door_id: int = Query(1)):
    """Return the next progressive hint for the specified door, voiced by the Genie."""
    global hint_counts
    
    doors = current_session.get("doors", [])
    door_index = door_id - 1

    if door_index < 0 or door_index >= len(doors):
        return {"hint": "No hint for you.", "audio_url": "", "hint_level": -1}

    door = doors[door_index]
    clues = door.get("clues", [])
    
    # Get current hint level (0, 1, 2) — clamp at max
    level = min(hint_counts.get(door_id, 0), len(clues) - 1)
    hint_text = clues[level] if clues else "Figure it out yourself!"
    
    # Advance hint counter for next time (cap at max)
    hint_counts[door_id] = min(level + 1, len(clues) - 1)
    
    # Generate voiced hint
    audio_file = f"hint_{door_id}_{level}.mp3"
    audio_path = os.path.join(STATIC_DIR, audio_file)
    
    # Wrap in Genie flavor
    if level == 0:
        spoken = f"You want a hint? Ugh, fine... {hint_text}"
    elif level == 1:
        spoken = f"STILL stuck? Wow. OK here's a better hint... {hint_text}"
    else:
        spoken = f"Oh for the love of — OK I'm practically GIVING it to you now! {hint_text}"
    
    print(f"🎙️ Generating hint (level {level}) for Door {door_id}...")
    generate_eleven_audio(spoken, audio_path)

    return {
        "hint": spoken,
        "audio_url": f"/static/{audio_file}",
        "hint_level": level,
        "hints_remaining": max(0, len(clues) - 1 - level)
    }


# =====================================================
# POST /process_wish — Judge a wish (merged: Ashley's door_rules + our voice)
# =====================================================
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
        print(f"User Wish for Door {door_id}: {user_wish}")

        # 2. Get Genie Judgment (uses dynamic door_rules from Unity)
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

        print("🎙️ Generating Drop Voice (ElevenLabs)...")
        generate_eleven_audio(genie_json.get("drop_voice", "I refuse."), drop_path)

        print("🎙️ Generating Verdict Voice (ElevenLabs)...")
        generate_eleven_audio(genie_json.get("congrats_voice", "Fine, you pass."), congrats_path)

        # --- 4. MAP TO URLS ---
        genie_json["audio_url_drop"] = f"/static/{drop_file}"
        genie_json["audio_url_congrats"] = f"/static/{congrats_file}"
        genie_json["audio_url"] = genie_json["audio_url_congrats"] if genie_json.get("door_open") else genie_json["audio_url_drop"]

        print(f"✅ Drop: {genie_json['audio_url_drop']}")
        print(f"✅ Congrats: {genie_json['audio_url_congrats']}")

        return genie_json

    except Exception as e:
        print(f"❌ Error: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))
    finally:
        if os.path.exists(temp_filename):
            os.remove(temp_filename)


# =====================================================
# GET /intro — Genie welcome monologue (uses first clue from Door 1)
# =====================================================
INTRO_TEMPLATE = (
    "OH! Oh oh oh — you're here! You're actually here! "
    "I've been waiting — do you know how long I've been stuck in this room? Don't answer that. "
    "OK so — bad news — you're trapped. Yeah. Sorry about that! "
    "BUT! That door? Right there? That's your ticket out. "
    "The catch — because there's ALWAYS a catch — "
    "{clue} "
    "Just hold the trigger on your controller... talk to me... make a wish... "
    "and MAYBE I'll help you. Maybe. No promises!"
)

@app.get("/intro")
def get_intro():
    """Generate the Genie's intro monologue, including Door 1's first (hard) clue"""
    # Get Door 1's first clue (hard) from session
    clue = "well, you'll just have to figure it out yourself!"
    doors = current_session.get("doors", [])
    if doors:
        clues = doors[0].get("clues", [])
        if clues:
            clue = clues[0]  # First clue = hard/cryptic
    
    intro_text = INTRO_TEMPLATE.format(clue=clue)
    intro_path = os.path.join(STATIC_DIR, "intro.mp3")

    # Always regenerate (cached audio was cleared by /get_rules)
    print("🎙️ Generating Genie intro monologue...")
    success = generate_eleven_audio(intro_text, intro_path)
    if not success:
        raise HTTPException(status_code=500, detail="Failed to generate intro audio")
    print("✅ Intro audio generated")

    return {
        "audio_url": "/static/intro.mp3",
        "subtitle": intro_text
    }


# =====================================================
# GET /room_transition — Congrats + next room's first clue
# =====================================================
@app.get("/room_transition")
def room_transition(door_id: int = 2):
    """Generate voiced congrats + next room's first (hard) clue"""
    doors = current_session.get("doors", [])
    door_index = door_id - 1  # door_id is 1-indexed

    if door_index < 0 or door_index >= len(doors):
        return {"audio_url": "", "subtitle": "You've escaped! ...somehow."}

    # Get the first (hard) clue for the new room
    clues = doors[door_index].get("clues", [])
    clue = clues[0] if clues else "Figure it out yourself!"
    
    transition_text = (
        f"Well well well — you actually made it! I'm... almost impressed. Almost. "
        f"But don't celebrate yet — room {door_id} is waiting, and here's your hint: "
        f"{clue} "
        f"Good luck — you'll DEFINITELY need it this time!"
    )

    audio_file = f"transition_{door_id}.mp3"
    audio_path = os.path.join(STATIC_DIR, audio_file)

    print(f"🎙️ Generating transition voice for room {door_id}...")
    generate_eleven_audio(transition_text, audio_path)

    return {
        "audio_url": f"/static/{audio_file}",
        "subtitle": transition_text
    }


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
