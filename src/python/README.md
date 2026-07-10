# rgGenAI Python — Enterprise GenAI Platform

State-of-the-art Python GenAI platform combining **RAG**, **LangChain**, **LangGraph**, and **MCP** (Model Context Protocol).

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     FastAPI REST API                        │
│  /api/health  /api/documents  /api/rag  /api/agents        │
└──────────────┬──────────────────────────┬────────────────────┘
               │                          │
    ┌──────────▼──────────┐    ┌──────────▼──────────┐
    │   LangGraph Agent   │    │    RAG Pipeline     │
    │  (ReAct + Tools)    │    │ Ingest → Embed →    │
    │  + SQLite Checkpts  │    │ Chroma Retrieval    │
    └──────────┬──────────┘    └──────────┬──────────┘
               │                          │
    ┌──────────▼──────────────────────────▼──────────┐
    │              LangChain + OpenAI                   │
    │         (Chat, Embeddings, Tools)                 │
    └──────────────────────────────────────────────────┘
               │
    ┌──────────▼──────────┐
    │    MCP Server       │  ← Cursor, Claude Desktop, etc.
    │  (stdio transport)  │
    └─────────────────────┘
```

## Stack

| Layer | Technology |
|-------|-----------|
| API | FastAPI + Uvicorn + SSE streaming |
| Agents | LangGraph (ReAct graph, SQLite checkpoints) |
| Chains/Tools | LangChain (tools, prompts, embeddings) |
| RAG | ChromaDB + OpenAI embeddings + PDF/text ingestion |
| MCP | Official `mcp` SDK (stdio server + client utilities) |
| LLM | OpenAI (gpt-4o-mini default, configurable) |

## Quick Start (Local)

### Prerequisites

- Python 3.11+
- OpenAI API key

### 1. Setup

```bash
cd src/python
chmod +x scripts/*.sh
./scripts/setup.sh
```

### 2. Configure

Edit `.env` and set your API key:

```bash
OPENAI_API_KEY=sk-your-actual-key
```

### 3. Run API Server

```bash
source .venv/bin/activate
rggenai-api
```

Open **http://localhost:8000/docs** for interactive Swagger UI.

### 4. Try It

**Upload a document:**
```bash
curl -X POST http://localhost:8000/api/documents/upload \
  -F "file=@/path/to/document.pdf"
```

**RAG search:**
```bash
curl -X POST http://localhost:8000/api/rag/search \
  -H "Content-Type: application/json" \
  -d '{"query": "What is the main topic?"}'
```

**RAG Q&A (grounded answer):**
```bash
curl -X POST http://localhost:8000/api/rag/query \
  -H "Content-Type: application/json" \
  -d '{"question": "Summarize the key points"}'
```

**Run LangGraph agent:**
```bash
curl -X POST http://localhost:8000/api/agents/run \
  -H "Content-Type: application/json" \
  -d '{"message": "Search the knowledge base for release notes", "thread_id": "session-1"}'
```

## MCP Integration

The MCP server exposes tools to external AI clients (Cursor IDE, Claude Desktop, etc.).

### Run MCP Server

```bash
source .venv/bin/activate
rggenai-mcp
```

### Cursor IDE Configuration

Add to your Cursor MCP settings (see `mcp-config.example.json`):

```json
{
  "mcpServers": {
    "rggenai": {
      "command": "rggenai-mcp",
      "env": {
        "OPENAI_API_KEY": "sk-your-key"
      }
    }
  }
}
```

### Available MCP Tools

| Tool | Description |
|------|-------------|
| `rag_search` | Retrieve relevant document chunks with citations |
| `rag_query` | Full RAG pipeline — retrieve + LLM answer |
| `agent_run` | Run LangGraph agent with tool calling |

## Docker (Optional)

For production-like setup with remote Chroma:

```bash
cp .env.example .env   # set OPENAI_API_KEY
docker compose up -d
```

API: http://localhost:8000 | Chroma: http://localhost:8001

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/health` | Health check + component status |
| POST | `/api/documents/upload` | Upload PDF/TXT/MD for RAG indexing |
| POST | `/api/rag/search` | Similarity search with citations |
| POST | `/api/rag/query` | Grounded Q&A with LLM |
| POST | `/api/agents/run` | Run LangGraph research agent |
| POST | `/api/agents/stream` | SSE streaming agent response |
| DELETE | `/api/rag/index` | Reset vector index |

## Project Structure

```
src/python/
├── pyproject.toml          # Dependencies & scripts
├── docker-compose.yml      # Chroma + API containers
├── .env.example            # Environment template
├── mcp-config.example.json # Cursor MCP config
├── scripts/
│   ├── setup.sh            # One-command local setup
│   └── test.sh             # Run test suite
├── src/rggenai/
│   ├── main.py             # FastAPI entry point
│   ├── config.py           # Pydantic settings
│   ├── llm/                # LLM factory (LangChain)
│   ├── rag/                # Ingestion, vectorstore, retrieval
│   ├── agents/             # LangGraph ReAct agent
│   ├── mcp/                # MCP server + client
│   └── api/                # REST routes & schemas
└── tests/
```

## Development

```bash
./scripts/test.sh                    # Run tests
ruff check src/rggenai tests/        # Lint
ruff format src/rggenai tests/       # Format
```

## Configuration Reference

| Variable | Default | Description |
|----------|---------|-------------|
| `OPENAI_API_KEY` | — | **Required** for LLM/embeddings |
| `OPENAI_MODEL` | `gpt-4o-mini` | Chat model |
| `OPENAI_EMBEDDING_MODEL` | `text-embedding-3-small` | Embedding model |
| `CHROMA_PERSIST_DIR` | `./data/chroma` | Local vector store path |
| `RAG_CHUNK_SIZE` | `1000` | Document chunk size |
| `RAG_TOP_K` | `5` | Retrieval result count |
| `CHECKPOINT_DB_PATH` | `./data/checkpoints.db` | LangGraph conversation memory |

## License

MIT
