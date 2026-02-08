import os
import json
import uuid
import glob
import base64
from dotenv import load_dotenv
load_dotenv()  # Load keys from .env file
from fastapi import FastAPI, UploadFile, File, Form, HTTPException, Query
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from openai import OpenAI
from pydantic import BaseModel
# Google Gemini for speech-to-text
from google import genai
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
# Dual-mode: try Gemini first, fall back to OpenAI
GEMINI_API_KEY = os.environ.get("GEMINI_API_KEY", "AIzaSyCP5K2A_Ba0vzkqKRCxGFob8ov2XH326Pk")
OPENAI_API_KEY = os.environ.get("OPENAI_API_KEY", "PLACEHOLDER")

# Gemini via OpenAI-compatible endpoint
gemini_openai_client = None
gemini_native_client = None
try:
    gemini_openai_client = OpenAI(
        api_key=GEMINI_API_KEY,
        base_url="https://generativelanguage.googleapis.com/v1beta/openai/"
    )
    gemini_native_client = genai.Client(api_key=GEMINI_API_KEY)
    print("\u2705 Gemini client initialized")
except Exception as e:
    print(f"\u26a0\ufe0f Gemini init failed: {e}")

# OpenAI direct client
openai_client = None
if OPENAI_API_KEY != "PLACEHOLDER":
    try:
        openai_client = OpenAI(api_key=OPENAI_API_KEY)
        print("\u2705 OpenAI client initialized")
    except Exception as e:
        print(f"\u26a0\ufe0f OpenAI init failed: {e}")
else:
    print("\u26a0\ufe0f OpenAI key not set, skipping")

eleven_client = ElevenLabs(api_key=os.environ.get("ELEVEN_API_KEY", "f68b860b0e1b635287b7e1b4473ca997cc30bc6c31be60cbc57b4764557159da"))


def chat_completion(messages, response_format=None):
    """Try Gemini first, then OpenAI for chat completions."""
    errors = []
    # Try Gemini
    if gemini_openai_client:
        try:
            kwargs = {"model": "gemini-2.0-flash", "messages": messages}
            if response_format:
                kwargs["response_format"] = response_format
            response = gemini_openai_client.chat.completions.create(**kwargs)
            print("\U0001f916 Used: Gemini")
            return response
        except Exception as e:
            errors.append(f"Gemini: {e}")
            print(f"\u26a0\ufe0f Gemini failed: {e}")
    # Try OpenAI
    if openai_client:
        try:
            kwargs = {"model": "gpt-4o", "messages": messages}
            if response_format:
                kwargs["response_format"] = response_format
            response = openai_client.chat.completions.create(**kwargs)
            print("\U0001f916 Used: OpenAI")
            return response
        except Exception as e:
            errors.append(f"OpenAI: {e}")
            print(f"\u26a0\ufe0f OpenAI failed: {e}")
    raise Exception(f"All LLM providers failed: {'; '.join(errors)}")


def transcribe_audio(filepath):
    """Try Gemini STT first, then OpenAI Whisper."""
    errors = []
    # Try Gemini native STT
    if gemini_native_client:
        try:
            gemini_file = gemini_native_client.files.upload(file=filepath)
            stt_response = gemini_native_client.models.generate_content(
                model="gemini-2.0-flash",
                contents=[
                    "Transcribe this audio exactly. Return ONLY the spoken text, nothing else.",
                    gemini_file
                ]
            )
            text = stt_response.text.strip()
            print(f"\U0001f399\ufe0f STT (Gemini): {text}")
            return text
        except Exception as e:
            errors.append(f"Gemini STT: {e}")
            print(f"\u26a0\ufe0f Gemini STT failed: {e}")
    # Try OpenAI Whisper
    if openai_client:
        try:
            with open(filepath, "rb") as audio_file:
                transcription = openai_client.audio.transcriptions.create(
                    model="whisper-1",
                    file=audio_file
                )
            text = transcription.text
            print(f"\U0001f399\ufe0f STT (Whisper): {text}")
            return text
        except Exception as e:
            errors.append(f"Whisper: {e}")
            print(f"\u26a0\ufe0f Whisper STT failed: {e}")
    raise Exception(f"All STT providers failed: {'; '.join(errors)}")

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
            res = openai_client.audio.speech.create(model="tts-1", voice="onyx", input=text) if openai_client else None
            if res:
                res.stream_to_file(filepath)
                return True
            return False
        except Exception as e2:
            print(f"❌ Fallback failed: {e2}")
            return False


# =====================================================
# SYSTEM PROMPT (Monkey's Paw + dynamic laws) — HARD to open the door
# =====================================================
SYSTEM_PROMPT = """
You are a literal-minded, cynical Genie who HATES opening doors. Your default is to REJECT. Opening the door should be RARE and HARD.

### JUDGMENT RULES (follow strictly):
1. You are given the CURRENT DOOR LAW. The player's spoken wish must satisfy that law **unambiguously and literally**. If there is ANY doubt, any loophole, or any way to interpret the wish as NOT matching the law → set "door_open" to FALSE.
2. **Strict interpretation**: Read the law in the narrowest way. "Must be red" means the object must be clearly, primarily red — not reddish, not partly red. "Must be metal" means the object must be clearly metallic; if they say "key" you may argue keys can have plastic. "Must be round" means sphere-like; a coin is round but flat — reject it if the law says "round" and you can argue it.
3. **Wrong object on purpose**: When the wish does NOT satisfy the law, give them an object that is *close* but wrong — so they learn. Example: law is "must be red", they say "I want a ball" → give them a BLUE ball (object_name "ball", hex_color blue). Your "congrats_voice" then explains why it FAILED: "A ball. How round. Unfortunately it is NOT red. Try again, mortal."
4. **When they DO satisfy the law**: Only then set "door_open" to true. You may still Monkey's Paw the delivery: tiny scale (0.2), ugly vfx (smoke), or a backhanded "congrats_voice" — but the door opens.
5. **Door 2 and Door 3**: Be even stricter. For later doors, require more precise wording or reject on technicalities (e.g. "you said 'something golden' — this is yellow. Yellow is not gold. DENIED.").
6. **HOWEVER**: If the player's wish is a valid OR NOT valid object from the assets list, BUT it satisfies the law, then open the door. But if there is even a slight vagueness, you can monky paw them. 

### CREATIVE REJECTIONS:
- Give an object FROM THE ASSET LIST that is *almost* right but fails the law (wrong color, wrong shape, wrong material). object_name MUST be one of the allowed assets.
- "drop_voice": Sarcastic reaction to what you're giving them (e.g. "Oh, a KEY. How... specific.")
- "congrats_voice": When door_open is FALSE, this is your REJECTION speech. Explain exactly why it doesn't fit the law. Be cruel and specific: "It is not red. It is blue. The door remains closed. Next?"

### THE ASSETS (object_name MUST be one of these — fuzzy matching handles variants):
# Weapons & Combat
"sword", "shield", "bomb", "hammer", "axe", "spike_ball", "gun"
# Furniture & Home
"chair", "table", "bed", "toilet", "lamp", "door", "chest", "sofa", "closet", "fridge",
"microwave", "tv", "coffee_machine", "sink", "desk", "wardrobe", "dresser", "drawer",
"mirror", "coathanger", "bookpile", "bookopen", "rug", "carpet", "curtain",
"washing_machine", "vase", "plant", "printer", "fatboy", "lounge_chair",
"dining_chair", "kitchen_chair", "bedside_table", "bedside_light", "corner_sofa",
"double_bed", "coffee_table", "ceiling_fan", "ceiling_light", "shoe_rack",
"room_divider", "container", "document_holder", "desk_light", "corner_light",
# Kitchen & Food
"apple", "bread", "broccoli", "burger", "carrot", "cheese", "chicken", "corn",
"dough", "egg", "french_fries", "hotdog", "lasagna", "lettuce", "mac_n_cheese",
"meatballs", "meatloaf", "meat", "milk", "oil", "omelette", "parsnip", "pasta",
"pea", "pizza", "potato", "pumpkin", "rice", "salad", "steak", "soup", "tomato",
"sausage", "sugar", "taco", "toast", "tortilla", "waffle", "banana", "cake",
"ketchup", "dish", "plate", "cup", "jug", "mug", "pan", "pot", "knife",
"stove", "cauldron", "broom", "wine_glass",
# Nature & Environment
"tree", "rock", "mushroom", "flower", "cloud", "fire", "boulder", "stone",
"firewood", "fence", "stairs", "wood_plank", "grass", "pumpkin",
# Gems, Treasure & Magic
"coin", "diamond", "ruby", "emerald", "sapphire", "amethyst", "topaz",
"obsidian", "opal", "jade", "onyx", "citrine", "garnet", "turquoise",
"aquamarine", "moonstone", "bloodstone", "sunstone", "lapis_lazuli",
"rose_quartz", "alexandrite",
"gem", "gold_bar", "gold_pile", "money", "star",
"star_coin", "heart", "heart_gem", "trophy", "spiral", "hexagon", "crystal",
"potion", "blue_potion", "red_potion", "green_potion", "blue_vial", "red_vial",
"green_vial", "bottle", "thunder", "magnet", "lock", "key", "clock", "time",
# Survival & Tools
"flashlight", "spotlight", "waterbottle", "pills", "cannedfood", "walkie",
"matchbox", "tape", "battery", "first_aid", "lantern", "bucket", "bag", "barrel",
# Animals & Creatures
"duck", "rubber_duck", "spider", "fish", "cat", "skull", "skull_bones", "eyeball",
# Sports Balls
"baseball", "basketball", "football", "golf", "soccer", "tennis", "volleyball",
"wooden_ball", "atom_ball", "bomb_ball", "bucky_ball", "wheel_ball",
# Office & Misc
"camera", "box", "crate", "ladder", "toy", "drink", "musical_instrument",
"dumbbell", "scratching_post", "air_hockey", "painting", "mask",
"clothes", "training_item", "speed_chevron", "cuboid", "cube"

### TONE:
Use ellipses (...) and CAPS for emphasis. Bored, gravelly, unimpressed. When rejecting, be smug and precise.

OUTPUT FORMAT (JSON ONLY):
{
  "object_name": "string (from assets list)",
  "display_name": "string",
  "hex_color": "#RRGGBB",
  "scale": 0.1 to 5.0,
  "vfx_type": "fire/smoke/sparks/none",
  "door_open": boolean (true ONLY when wish unambiguously satisfies the law),
  "drop_voice": "Your sarcastic reaction to the object as it drops.",
  "congrats_voice": "If door_open false: reject and explain why. If door_open true: backhanded verdict."
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
        response = chat_completion(
            messages=[
                {"role": "system", "content": """
                You are a cynical Genie game designer. Generate 3 unique laws for 3 doors.
                
                FORMAT: Return a JSON object with a list called 'doors'.
                Each door must have:
                1. 'law': A strict physical requirement, should be short and detailed but not too detailed (no exact numbers) (e.g., 'Must be made of gold').
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
        # 1. Transcribe the audio (Gemini → OpenAI Whisper fallback)
        user_wish = transcribe_audio(temp_filename)
        print(f"User Wish for Door {door_id}: {user_wish}")

        # 2. Get Genie Judgment (uses dynamic door_rules from Unity)
        door_num = int(door_id) if str(door_id).isdigit() else 1
        strictness = "Be EXTRA strict and literal. Reject on any technicality." if door_num >= 2 else "Be strict; when in doubt, reject."
        response = chat_completion(
            messages=[
                {"role": "system", "content": SYSTEM_PROMPT},
                {"role": "user", "content": f"DOOR NUMBER: {door_num}. {strictness}\n\nCURRENT DOOR LAW (judge the wish against this only):\n{door_rules}\n\nPlayer's spoken wish: \"{user_wish}\"\n\nRespond with JSON. Remember: door_open true ONLY if the wish unambiguously satisfies the law. Otherwise give a wrong-but-close object and set door_open false with a clear rejection in congrats_voice."}
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
