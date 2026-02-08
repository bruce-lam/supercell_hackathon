#!/usr/bin/env bash
# Run the Genie backend for the Unity game.
# From project root: ./backend/run_backend.sh
# Or from backend: ./run_backend.sh

set -e
cd "$(dirname "$0")"

# Use venv if it exists
if [ -d "venv" ]; then
  source venv/bin/activate
elif [ -d "../venv" ]; then
  source ../venv/bin/activate
fi

# Check for API key (optional for local test; needed for voice/wish)
if [ -z "$OPENAI_API_KEY" ] || [ "$OPENAI_API_KEY" = "PLACEHOLDER" ]; then
  echo "⚠️  OPENAI_API_KEY not set. Set it for full Genie/voice features:"
  echo "   export OPENAI_API_KEY=sk-..."
  echo "   (Optional: ELEVEN_API_KEY for Genie voice)"
fi

echo "🧞 Starting Genie backend at http://localhost:8000"
echo "   Then open Unity and press Play."
python3 main.py
