# rgGenAI — Enterprise GenAI Platform

Production-grade GenAI platform with multi-LLM support, RAG, agents, and MCP integration.

## Stacks

### Python (recommended for AI/RAG/agents)

- FastAPI + LangChain + LangGraph + MCP
- ChromaDB vector store, OpenAI embeddings
- ReAct agent with tool calling and conversation checkpoints

```bash
cd src/python
./scripts/setup.sh
# Edit .env → set OPENAI_API_KEY
source .venv/bin/activate
rggenai-api
```

See [src/python/README.md](src/python/README.md) for full docs.

### .NET 9 (enterprise API)

- Clean Architecture Web API
- Semantic Kernel, PostgreSQL + pgvector
- OpenAI, Claude, Gemini

```bash
cd src/backend
dotnet restore GenAI.slnx
dotnet run --project GenAI.Api
```

## Repository Structure

```
src/
├── python/          # Python GenAI platform (RAG, LangGraph, MCP)
└── backend/         # .NET 9 GenAI platform (Semantic Kernel)
```

## Features

- Multi-model chat with streaming
- RAG: document upload, chunking, embeddings, similarity search, citations
- LangGraph agent framework with tool calling
- MCP server for Cursor / Claude Desktop integration
- Release Coordinator Agent (.NET): Jira → summary → ServiceNow CR

## Configuration

**Python:** Copy `src/python/.env.example` to `.env` and set `OPENAI_API_KEY`.

**.NET:** Set provider API keys under the `LLM` section in appsettings. Jira and ServiceNow use mock data by default (`UseMockData: true`).
