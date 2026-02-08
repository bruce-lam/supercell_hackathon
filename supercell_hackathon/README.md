# 🧞 Hypnagogia — The AI Escape Room

> **Category: AI-Game** — A voice-powered VR escape room built with Generative AI at its core.

**Hypnagogia** is a game where you negotiate with a cynical, wish-granting Genie to escape three enchanted rooms. You speak your wishes aloud, the Genie interprets them, drops objects from a magical pipe, and decides — with plenty of sarcasm — whether your logic is worthy of passage. Every interaction is powered entirely by Generative AI. The game cannot exist without it.

![Unity](https://img.shields.io/badge/Unity_6-000000?style=flat&logo=unity)
![GPT-4o](https://img.shields.io/badge/GPT--4o-412991?style=flat&logo=openai)
![ElevenLabs](https://img.shields.io/badge/ElevenLabs-000000?style=flat)
![Python](https://img.shields.io/badge/FastAPI-009688?style=flat&logo=fastapi)

---

## 🚀 Quick Start (Judges)

### You Will Need
- **Unity 6** (6000.0+)
- **Python 3.11+**
- **OpenAI API Key** — [platform.openai.com/api-keys](https://platform.openai.com/api-keys)
- **ElevenLabs API Key** — [elevenlabs.io/app/settings/api-keys](https://elevenlabs.io/app/settings/api-keys)

### Step 1: Start the Backend

```bash
cd backend

# Create and activate a virtual environment
python3 -m venv venv
source venv/bin/activate        # macOS/Linux
# venv\Scripts\activate          # Windows

# Install dependencies
pip install -r requirements.txt

# Create your .env file with your API keys
cat > .env <<EOF
OPENAI_API_KEY=sk-your-openai-key-here
ELEVEN_API_KEY=your-elevenlabs-key-here
EOF

# Start the server
python main.py
```

You should see: `🧞 Genie backend running at http://localhost:8000`

### Step 2: Open Unity

1. Open the project in **Unity 6**
2. Open the scene: `Assets/Hypnagogia_Main.unity`
3. Run **Hypnagogia → Populate All Items Into Pipes** (top menu bar) to load all 449 item prefabs
4. **Save the scene** (Cmd+S / Ctrl+S)
5. Press **Play** ▶️
6. The Genie will introduce itself — listen and then **speak your wish** into your microphone

### Step 3: Play!

- 🎙️ **Speak naturally** — "I wish for something red and heavy"
- 📦 Watch the object tumble from the pipe
- 🗣️ Listen to the Genie roast you
- 🚪 Walk to the door — if the Genie approves, it swings open with a creak

---

## 🎮 How It Works

You're trapped in a mystical room with **three locked doors**, each governed by a secret law. A magical pipe hangs from the ceiling, and a Genie controls what falls out.

1. **🎙️ Speak a wish** — "I wish for something heavy and metal!"
2. **🧠 The Genie judges** — GPT-4o interprets your wish, picks an object, and evaluates it against the door's hidden law
3. **📦 An object drops from the pipe** — One of 449 3D models tumbles out with full physics
4. **🗣️ The Genie voices its verdict** — ElevenLabs synthesizes sarcastic dialogue in real-time
5. **🚪 The door opens... or doesn't** — If your logic satisfied the law, you escape. If not, the Genie gloats.

The Genie is a *Monkey's Paw* — it deliberately misinterprets wishes. Ask for "something red" and it might drop a red fish. Ask for "a weapon" and it might give you a rubber duck. You must be precise.

### The Three Door Laws (Hidden from the player)

| Door | Law | What Opens It |
|------|-----|---------------|
| 🚪 Door 1 | **The Law of Form** | Something light + portable |
| 🚪 Door 2 | **The Law of Substance** | Something massive + metal |
| 🚪 Door 3 | **The Law of Purpose** | Something with specific intent |

---

## 🤖 How Generative AI Powers Everything

This game uses **Generative AI in every layer** — not as a feature, but as the foundation. Without GenAI, this game literally cannot function. There are no scripted responses, no pre-recorded dialogue, no hardcoded puzzle solutions.

### In-Game: Three AI Systems in Real-Time

#### 1. 🎙️ OpenAI Whisper — Hearing the Player
- Players speak naturally into their microphone — no menus, no typing
- Whisper transcribes speech to text in real-time
- Supports natural language: "I want a big shiny golden shield" works just as well as "shield"

#### 2. 🧠 GPT-4o — The Genie's Brain
- Receives the player's transcribed wish + the current door's hidden law
- Interprets intent through a sarcastic "Monkey's Paw" persona
- Selects an object from **449 items** across 15 asset categories
- Decides color, scale, VFX effects, and whether the wish satisfies the law
- Generates **two dialogue scripts**: a drop reaction and a door verdict
- Returns structured JSON to Unity:

```json
{
  "object_name": "shield",
  "display_name": "A Battered Iron Shield",
  "hex_color": "#8B7355",
  "scale": 1.2,
  "vfx_type": "sparks",
  "door_open": true,
  "drop_voice": "A shield? How... predictably medieval of you.",
  "congrats_voice": "Fine. It's metal. Heavy. The door relents."
}
```

#### 3. 🗣️ ElevenLabs — The Genie's Voice
- GPT-4o's scripts are synthesized into speech via ElevenLabs TTS
- Uses `eleven_turbo_v2_5` for low-latency real-time feel
- Custom voice tuning for the Genie persona (gravelly, bored, sarcastic)
- Produces two audio clips per wish, plus intro narration and room transition dialogue
- Background music automatically ducks when the Genie speaks

#### The AI Pipeline (per wish)

```
Player speaks → [Whisper STT] → text
                                  ↓
                          [GPT-4o reasoning] → JSON (object + dialogue)
                                  ↓
                    ┌─────────────┼─────────────┐
                    ↓                           ↓
          [ElevenLabs TTS]            [Unity spawns object]
          drop_voice.mp3              3D model + physics
          verdict_voice.mp3           colliders + VFX
                    ↓                           ↓
              Audio plays ←──── Player interacts with door
```

### In Development: AI-Assisted Game Building

Beyond gameplay, **Generative AI was used extensively to build the game itself**:

| Tool | What It Did |
|------|-------------|
| **Google Gemini (Antigravity)** | Pair-programmed the entire codebase — Unity scripts, editor tools, procedural textures, audio integration, material debugging, backend API, this README, and more. Acted as a real-time co-developer across every aspect of the game. |
| **ElevenLabs Sound Effects API** | Generated door sound effects (wooden creak, heavy thud) from text descriptions — no need to source audio files manually |
| **GPT-4o** | Designed the Genie persona, puzzle laws, and rejection dialogue patterns |

**Every line of code in this project was written or co-written with GenAI assistance.** The game's architecture, puzzle logic, audio pipeline, material system, editor automation, and deployment scripts were all developed collaboratively with AI tools. This is a game that was not only powered by GenAI at runtime, but built by GenAI during development.

---

## ♿ Accessibility: A Game for Vision-Impaired Players

Hypnagogia has a unique quality: **nearly all gameplay interaction happens over voice**.

- 🎙️ **Input** is spoken — no need to read menus, click buttons, or navigate UI
- 🗣️ **Output** is spoken — the Genie narrates everything: what dropped, why the door stayed closed, what to try next
- 📝 **Subtitles** are displayed with dynamic duration for hearing-impaired players
- 🚶 **Movement** is the only visual requirement — walking forward to the door

This makes Hypnagogia a strong candidate for **accessible gaming**. A vision-impaired player can fully experience the puzzle-solving, humor, and satisfaction of the game through voice alone. The Genie describes every object, explains every rejection, and narrates every room transition.

With minor additions (audio-based spatial cues for navigation), this game could be fully playable without sight — a rare quality in 3D gaming.

---

## 🏗️ Architecture

```
┌─────────────────────────────────────┐
│           Unity 6 (Client)          │
│                                     │
│  GenieClient.cs     ← Mic + wishes  │
│  GenieIntro.cs      ← Intro narr.   │
│  PipeSpawner.cs     ← Fuzzy match   │
│  BackgroundMusic.cs ← BGM + ducking │
│  DoorInitializer.cs ← Door states   │
│                                     │
│  449 Item Prefabs across 15 packs   │
│  URP materials + runtime VFX        │
└──────────────┬──────────────────────┘
               │ HTTP POST /process_wish
               ↓
┌─────────────────────────────────────┐
│        FastAPI Backend (Python)     │
│                                     │
│  Whisper  → Speech-to-Text          │
│  GPT-4o   → Judgment + Dialogue     │
│  ElevenLabs → Voice Synthesis       │
│                                     │
│  Returns: JSON + streamed audio     │
└─────────────────────────────────────┘
```

---

## 🎨 Visual & Audio Design

- **Three themed escape rooms** with unique atmospheres
- **449 spawnable 3D objects** from 15 asset packs — weapons, food, furniture, gems, sports equipment, survival tools, animals, and more
- **Fuzzy name matching** — GPT says "chair" and Unity finds the best match from `chair`, `(Prb)Chair1`, `kitchen_chair_001`, `lounge_chair_001`
- **Runtime particle VFX** — fire (orange), smoke (gray), sparks (yellow) using URP-compatible materials
- **Background music** with automatic ducking when the Genie speaks
- **Sound effects** — wooden door creak on open, generated via ElevenLabs SFX API
- **Dynamic subtitles** — duration scales with text length, stays visible during audio

---

## 📁 Project Structure

```
supercell_hackathon/
├── Assets/
│   ├── Scripts/
│   │   ├── GenieClient.cs             # Voice capture, wish pipeline, door logic
│   │   ├── GenieIntro.cs              # Genie intro narration
│   │   ├── PipeSpawner.cs             # 3-tier fuzzy matching + physics spawning
│   │   ├── BackgroundMusic.cs         # Looping BGM with volume ducking
│   │   ├── DoorInitializer.cs         # Forces all doors closed on start
│   │   └── Editor/
│   │       ├── PopulateAllItems.cs    # Scans 15 packs → 449 prefabs
│   │       ├── SetupMaterials.cs      # Procedural room textures
│   │       └── DoorStateCopier.cs     # Door state debugging tools
│   ├── Prefabs/Items/                 # 449 game-ready item prefabs
│   ├── Audio/                         # BGM + door SFX
│   └── [15 Asset Packs]/             # BTM, Pandazole, Ball Pack, etc.
├── backend/
│   ├── main.py                        # FastAPI: Whisper + GPT-4o + ElevenLabs
│   ├── requirements.txt               # Python dependencies
│   ├── run_backend.sh                 # One-command launcher
│   └── .env                           # API keys (not committed)
└── README.md
```

---

## 👥 Team

Built at the **Supercell Hackathon 2025** 🏆

---

## 📜 License

Open-sourced for all hackathon participants as per competition rules.
