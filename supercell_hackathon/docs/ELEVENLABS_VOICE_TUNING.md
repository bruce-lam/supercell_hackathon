# 🎙️ ElevenLabs Voice Tuning Guide for Hypnagogia

## Current Setup

| Setting | Value |
|---------|-------|
| **Voice** | Adam (`pNInz6obpgDQGcFmaJgB`) |
| **Model** | `eleven_turbo_v2_5` (low-latency) |
| **Output** | `mp3_44100_128` |
| **Env Var** | `ELEVEN_API_KEY`, `ELEVEN_VOICE_ID` |

---

## Step 1: Pick a Voice

1. Go to [ElevenLabs Voice Library](https://elevenlabs.io/voice-library)
2. Search by tags: **"Old"**, **"Deep"**, **"Narrative"**, **"Character"**, **"Gritty"**
3. Recommended voices for a cynical Genie:

| Voice | Style | Voice ID |
|-------|-------|----------|
| **Adam** (current) | Deep, authoritative | `pNInz6obpgDQGcFmaJgB` |
| **Fin** | Energetic, gritty | Search in library |
| **Marcus** | Authoritative, deep | `5N2L9QnjwAaI53acX32s` |
| **Arnold** | Gravelly, cinematic | Search in library |

4. Click **"Add to VoiceLab"** → copy the **Voice ID**

---

## Step 2: Apply the Voice

### Option A: Environment Variable (no code change)
```bash
ELEVEN_VOICE_ID="your_voice_id_here" \
ELEVEN_API_KEY="your_key" \
OPENAI_API_KEY="your_key" \
python main.py
```

### Option B: Edit `main.py` line 37
```python
GENIE_VOICE_ID = os.environ.get("ELEVEN_VOICE_ID", "your_voice_id_here")
```

---

## Step 3: Tune Voice Settings (ElevenLabs Dashboard)

Go to **VoiceLab** → click your voice → adjust sliders:

### Stability (🎯 Set to ~30%)
| Value | Effect |
|-------|--------|
| **Low (20-35%)** | Erratic, emotional, dramatic — **best for a magical Genie** |
| **Medium (50%)** | Balanced, natural |
| **High (70-100%)** | Robotic, monotone news anchor — avoid for character work |

### Similarity Boost (🎯 Set to ~75%)
| Value | Effect |
|-------|--------|
| **Low** | More variation, less like the original voice |
| **High (75%+)** | Stays close to the original voice sample |

### Style Exaggeration (🎯 Set to ~40%)
| Value | Effect |
|-------|--------|
| **0%** | Neutral delivery |
| **40%+** | More dramatic, theatrical — good for sarcastic Genie |

---

## Step 4: Model Selection

In `main.py`, the `generate_eleven_audio` function uses:

```python
model_id="eleven_turbo_v2_5"  # Current: fast, ~0.3s latency
```

| Model | Latency | Quality | Best For |
|-------|---------|---------|----------|
| `eleven_turbo_v2_5` | ~300ms | Good | **Game feel (current)** |
| `eleven_multilingual_v2` | ~1-2s | Best | Max dramatic quality |
| `eleven_monolingual_v1` | ~500ms | Good | English-only, reliable |

To switch, change the `model_id` string in `main.py` line 47.

---

## Step 5: Prompt Engineering for Better Acting

The Genie's voice quality depends on the **text GPT-4o generates**. Punctuation guides intonation:

| Technique | Example | Effect |
|-----------|---------|--------|
| Ellipses `...` | `"A rock... how original."` | Dramatic pause |
| Exclamation `!` | `"You dare bring me a BANANA!"` | Emphasis |
| ALL CAPS | `"The door stays SHUT."` | Stressed word |
| Question mark | `"Did you actually think that would work?"` | Rising intonation |
| Em dash `—` | `"Heavy? Yes. Metal? No — the door stays shut."` | Quick pause |

These are already baked into the system prompt via GPT-4o's response style, but you can enhance them by tweaking the `SYSTEM_PROMPT` in `main.py`.

---

## Quick Test

Restart the backend after any changes:
```bash
# Kill old server
lsof -ti :8000 | xargs kill -9

# Start with new voice
OPENAI_API_KEY="your_key" \
ELEVEN_API_KEY="your_key" \
ELEVEN_VOICE_ID="new_voice_id" \
python main.py
```

Then make a wish in-game. Check the console for:
```
🎙️ Generating Drop Voice (ElevenLabs)...
🎙️ Generating Verdict Voice (ElevenLabs)...
```

If ElevenLabs fails, it auto-falls back to OpenAI TTS (you'll see a `⚠️ ElevenLabs Error` log).
