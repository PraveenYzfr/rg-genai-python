#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
cd "$PROJECT_DIR"

echo "==> rgGenAI Python — Local Setup"
echo ""

if [ ! -f .env ]; then
  echo "Creating .env from .env.example..."
  cp .env.example .env
  echo "⚠️  Edit .env and set OPENAI_API_KEY before running agents/RAG queries."
fi

mkdir -p data uploads

echo "==> Creating virtual environment..."
python3 -m venv .venv
source .venv/bin/activate

echo "==> Installing dependencies..."
pip install --upgrade pip
pip install -e ".[dev]"

echo ""
echo "✅ Setup complete!"
echo ""
echo "Next steps:"
echo "  1. Edit .env and set OPENAI_API_KEY"
echo "  2. source .venv/bin/activate"
echo "  3. rggenai-api          # Start API server at http://localhost:8000"
echo "  4. Open http://localhost:8000/docs for Swagger UI"
echo ""
echo "Optional — run MCP server (for Cursor/Claude Desktop):"
echo "  rggenai-mcp"
echo ""
echo "Optional — Docker with Chroma:"
echo "  docker compose up -d"
