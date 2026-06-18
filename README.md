# rgGenAI — Enterprise GenAI Platform

Production-grade .NET 9 GenAI platform with multi-LLM support, RAG (pgvector), and Semantic Kernel agents.

## Stack

- .NET 9 Web API (Clean Architecture)
- Semantic Kernel
- PostgreSQL + pgvector
- OpenAI, Claude, Gemini

## Features

- Multi-model chat with streaming
- RAG: PDF upload, chunking, embeddings, similarity search, citations
- Agent framework with tool calling
- Release Coordinator Agent (Jira → summary → ServiceNow CR)

## Quick Start

```bash
cd src/backend
dotnet restore GenAI.slnx
dotnet run --project GenAI.Api
```

### Prerequisites

- PostgreSQL with `CREATE EXTENSION vector;`
- API keys in `GenAI.Api/appsettings.Development.json`

### Key Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /api/health` | Health check |
| `POST /api/llm/complete` | LLM completion |
| `POST /api/llm/stream` | SSE streaming |
| `POST /api/documents/upload` | Upload PDF for RAG |
| `POST /api/rag/search` | Similarity search |
| `POST /api/agents/run` | Run agent |

## Project Structure

```
src/backend/
├── GenAI.Api/            # Web API host
├── GenAI.Application/    # Use cases, interfaces
├── GenAI.Domain/         # Entities
├── GenAI.Infrastructure/ # EF Core, PostgreSQL, integrations
├── GenAI.AI/             # LLM providers, agents, plugins
└── GenAI.RAG/            # Document ingestion, embeddings, retrieval
```

## Configuration

Set provider API keys under the `LLM` section in appsettings. Jira and ServiceNow use mock data by default (`UseMockData: true`).
