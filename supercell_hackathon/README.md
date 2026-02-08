# 🧞 Hypnagogia — The AI Escape Room

> **Category: AI-Game** — A brand-new game built with GenAI at its core.

**Hypnagogia** is a voice-powered escape room where you negotiate with a cynical, wish-granting Genie to unlock three enchanted doors. Every interaction — from understanding your words to judging your logic to voicing sarcastic insults — is powered by AI. The game is **impossible without GenAI**.

![Unity](https://img.shields.io/badge/Unity_6-000000?style=flat&logo=unity)
![GPT-4o](https://img.shields.io/badge/GPT--4o-412991?style=flat&logo=openai)
![ElevenLabs](https://img.shields.io/badge/ElevenLabs-000000?style=flat)
![Python](https://img.shields.io/badge/FastAPI-009688?style=flat&logo=fastapi)

---

## 🎮 How It Works

You're trapped in a mystical room with **three locked doors**, each governed by a hidden law. A magical pipe hangs from the ceiling, and a Genie (who hates you) controls it.

1. **🎙️ Speak a wish** — "I wish for something heavy and metal!"
2. **🧠 The Genie judges** — GPT-4o interprets your wish, picks an object, and decides if it satisfies the door's law
3. **📦 An object falls from the pipe** — A 3D model tumbles out with physics
4. **🗣️ The Genie roasts you** — ElevenLabs voices a sarcastic comment about your wish
5. **🚪 The door opens... or doesn't** — If your logic was sound, you escape. If not, the Genie mocks you further.

### The Three Door Laws (Hidden from the player)

| Door | Law | What Opens It |
|------|-----|---------------|
| 🚪 Door 1 | **The Law of Form** | Something light + portable |
| 🚪 Door 2 | **The Law of Substance** | Something massive + metal |
| 🚪 Door 3 | **The Law of Purpose** | Something with specific intent |

---

## 🤖 AI Integration — Three GenAI Systems Working Together

This game uses **three distinct GenAI systems** in a real-time pipeline. No scripted dialogue, no pre-recorded audio, no hardcoded puzzle solutions — everything is generated live.

### 1. 🎙️ OpenAI Whisper — Speech-to-Text
- Players speak naturally into their microphone
- Whisper transcribes the audio in real-time
- No menus, no typing — pure voice interaction

### 2. 🧠 OpenAI GPT-4o — The Genie's Brain
- Receives the transcribed wish + door context
- Interprets the player's intent through a "Monkey's Paw" persona
- Selects an object from the asset library (33 items)
- Determines if the wish satisfies the door's hidden law
- Generates **two voice scripts**: a drop reaction + a verdict
- Returns structured JSON with object, color, scale, VFX, and dialogue

```json
{
  "object_name": "shield",
  "display_name": "A Battered Iron Shield",
  "hex_color": "#8B7355",
  "scale": 1.2,
  "vfx_type": "sparks",
  "door_open": true,
  "drop_voice": "A shield? How... predictably medieval of you.",
  "congrats_voice": "Fine. It's metal. Heavy. The door relents. Don't let it hit you on the way out."
}
```

### 3. 🗣️ ElevenLabs — The Genie's Voice
- GPT-4o's scripts are sent to ElevenLabs TTS
- Uses the `eleven_turbo_v2_5` model for low-latency game feel
- Produces **two audio clips per wish**: the roast (when the object drops) and the verdict (when testing the door)
- Falls back to OpenAI TTS if ElevenLabs is unavailable

### The AI Pipeline (per wish)

```
Player speaks → [Whisper STT] → text
                                  ↓
                          [GPT-4o reasoning] → JSON (object + dialogue)
                                  ↓
                    ┌─────────────┼─────────────┐
                    ↓                           ↓
          [ElevenLabs TTS]            [Unity spawns object]
          drop_voice.mp3              3D model + physics
          verdict_voice.mp3           colliders + labels
                    ↓                           ↓
              Audio plays ←──── Player interacts with door
```

---

## 🏗️ Architecture

```
┌─────────────────────────────────────┐
│           Unity 6 (Client)          │
│                                     │
│  GenieClient.cs    ← Mic capture    │
│  PipeSpawner.cs    ← Object spawn   │
│  WishManager.cs    ← Wish flow      │
│  NetworkManager.cs ← HTTP to backend│
│                                     │
│  3D Assets: Synty Polygon Starter   │
│  + Procedural textures & materials  │
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
│  Returns: JSON + audio URLs         │
└─────────────────────────────────────┘
```

---

## 🎨 Visual Design

The escape room features a **procedurally generated dungeon aesthetic**:

- **Stone brick walls** with per-brick color variation and deep mortar grooves
- **Checkered dungeon floor** with metallic reflections
- **Glowing magic rug** under the item pipe (emissive purple)
- **Atmospheric mood lighting** — cyan, pink, warm, and purple point lights
- **3D item models** from Synty Polygon Starter Pack (sword, shield, tree, crate, etc.)
- **33 spawnable items** across 7 categories: weapons, furniture, nature, food, animals, tools, shapes

---

## 🚀 Quick Start

### Prerequisites
- Unity 6  
- Python 3.11+
- OpenAI API key
- ElevenLabs API key

### Backend Setup
```bash
cd backend
python -m venv venv
source venv/bin/activate
pip install fastapi uvicorn openai python-multipart elevenlabs

# Run with API keys
OPENAI_API_KEY="your-key" ELEVEN_API_KEY="your-key" python main.py
```

### Unity Setup
1. Open the project in Unity 6
2. Run **Hypnagogia → Setup Room (Rebuild)** to generate the environment
3. Run **Hypnagogia → Generate Item Prefabs** to create spawn items
4. Run **Hypnagogia → Assign Items to Pipe Spawner** to wire up the spawner
5. Press **Play** — speak a wish and press **F** to test item spawning

---

## 🛠️ AI-Powered Development Tools

Beyond gameplay, AI was used extensively in the **development process**:

| Tool | Usage |
|------|-------|
| **Gemini (Antigravity)** | Pair-programmed the entire Unity editor scripting pipeline, procedural texture generation, asset mapping, material debugging, and this README |
| **GPT-4o** | Core game logic engine — interprets wishes, enforces door laws, generates dialogue |
| **OpenAI Whisper** | Real-time speech-to-text for hands-free gameplay |
| **ElevenLabs** | Character voice synthesis for the Genie persona |

---

## 📁 Project Structure

```
supercell_hackathon/
├── Assets/
│   ├── Scripts/
│   │   ├── GenieClient.cs          # Mic capture + backend communication  
│   │   ├── PipeSpawner.cs          # Physics-based item spawning
│   │   ├── WishManager.cs          # Wish workflow orchestration
│   │   ├── NetworkManager.cs       # HTTP client for FastAPI backend
│   │   └── Editor/
│   │       ├── SetupMaterials.cs   # Room generation + procedural textures
│   │       ├── ItemPrefabGenerator.cs  # 33 item prefab factory
│   │       ├── SyntyAssetMapper.cs     # Auto-maps 3D models to items
│   │       └── SyntyMaterialRepair.cs  # Fixes third-party material issues
│   ├── Prefabs/Items/              # Generated item prefabs
│   ├── Materials/                  # Procedural textures (floor, walls)
│   └── PolygonStarter/             # Synty 3D asset pack
├── backend/
│   ├── main.py                     # FastAPI server (Whisper + GPT-4o + ElevenLabs)
│   └── static/                     # Generated audio files
└── README.md
```

---

## 👥 Team

Built at the **Supercell Hackathon 2025** 🏆

---

## 📜 License

Open-sourced for all hackathon participants as per competition rules.
