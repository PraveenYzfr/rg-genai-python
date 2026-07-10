"""FastAPI route handlers."""

import json
from collections.abc import AsyncGenerator

from fastapi import APIRouter, File, HTTPException, UploadFile
from sse_starlette.sse import EventSourceResponse

from rggenai import __version__
from rggenai.agents.graph import get_research_agent
from rggenai.api.schemas import (
    AgentRunRequest,
    AgentRunResponse,
    DocumentUploadResponse,
    HealthResponse,
    ProviderInfo,
    ProvidersResponse,
    RagQueryRequest,
    RagQueryResponse,
    RagSearchRequest,
    RagSearchResponse,
)
from rggenai.config import get_settings
from rggenai.llm.providers import (
    list_provider_status,
    resolve_chat_provider,
)
from rggenai.rag.pipeline import RagPipeline
from rggenai.rag.service import RagService
from rggenai.rag.vectorstore import get_vector_store

router = APIRouter(prefix="/api")


def _get_rag_service() -> RagService:
    return RagService()


def _get_rag_pipeline() -> RagPipeline:
    return RagPipeline()


@router.get("/health", response_model=HealthResponse)
async def health() -> HealthResponse:
    settings = get_settings()
    components: dict[str, str] = {"vector_store": "unknown"}

    try:
        count = get_vector_store().document_count()
        components["vector_store"] = f"ok ({count} chunks)"
    except Exception as exc:
        components["vector_store"] = f"error: {exc}"

    for provider_info in list_provider_status(settings):
        pid = provider_info["id"]
        chat = "ready" if provider_info["chat_available"] else "not configured"
        embed = "ready" if provider_info["embeddings_available"] else "not configured"
        components[f"llm_{pid}"] = f"chat: {chat}, embeddings: {embed}"

    try:
        resolve_chat_provider(settings)
        status = "healthy"
    except ValueError:
        status = "degraded"

    return HealthResponse(
        status=status,
        version=__version__,
        components=components,
        default_llm_provider=settings.default_llm_provider,
        default_embedding_provider=settings.default_embedding_provider,
    )


@router.get("/providers", response_model=ProvidersResponse)
async def list_providers() -> ProvidersResponse:
    settings = get_settings()
    providers = [ProviderInfo(**p) for p in list_provider_status(settings)]
    return ProvidersResponse(
        providers=providers,
        usage_hint=(
            "Set DEFAULT_LLM_PROVIDER in .env to switch globally, "
            "or pass 'provider' in /api/rag/query and /api/agents/run requests. "
            "Free options: groq (fast), gemini (free tier), ollama (local)."
        ),
    )


@router.post("/documents/upload", response_model=DocumentUploadResponse)
async def upload_document(file: UploadFile = File(...)) -> DocumentUploadResponse:
    if not file.filename:
        raise HTTPException(status_code=400, detail="Filename is required")

    allowed = {".pdf", ".txt", ".md", ".markdown"}
    suffix = "." + file.filename.rsplit(".", 1)[-1].lower() if "." in file.filename else ""
    if suffix not in allowed:
        raise HTTPException(
            status_code=400,
            detail=f"Unsupported file type. Allowed: {', '.join(sorted(allowed))}",
        )

    content = await file.read()
    if not content:
        raise HTTPException(status_code=400, detail="Empty file")

    try:
        result = _get_rag_pipeline().ingest_upload(file.filename, content)
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc

    return DocumentUploadResponse(**result)


@router.post("/rag/search", response_model=RagSearchResponse)
async def rag_search(request: RagSearchRequest) -> RagSearchResponse:
    result = _get_rag_service().retrieve(request.query, top_k=request.top_k)
    return RagSearchResponse(
        query=result.query,
        documents=[
            {"content": doc.page_content, "metadata": doc.metadata}
            for doc in result.documents
        ],
        citations=[
            {
                "source": c.source,
                "content_preview": c.content_preview,
                "score": c.score,
            }
            for c in result.citations
        ],
    )


@router.post("/rag/query", response_model=RagQueryResponse)
async def rag_query(request: RagQueryRequest) -> RagQueryResponse:
    settings = get_settings()
    try:
        result = await _get_rag_service().query(
            request.question,
            top_k=request.top_k,
            provider=request.provider,
        )
        used_provider = resolve_chat_provider(settings, request.provider).value
    except ValueError as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc

    return RagQueryResponse(
        query=result.query,
        answer=result.answer,
        citations=[
            {
                "source": c.source,
                "content_preview": c.content_preview,
                "score": c.score,
            }
            for c in result.citations
        ],
        chunks_retrieved=result.chunks_retrieved,
        provider=used_provider,
    )


@router.post("/agents/run", response_model=AgentRunResponse)
async def run_agent(request: AgentRunRequest) -> AgentRunResponse:
    try:
        agent = get_research_agent(provider=request.provider)
        result = await agent.run(request.message, thread_id=request.thread_id)
    except ValueError as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc

    return AgentRunResponse(**result)


@router.post("/agents/stream")
async def stream_agent(request: AgentRunRequest):
    agent = get_research_agent(provider=request.provider)

    async def event_generator() -> AsyncGenerator[dict, None]:
        try:
            async for event in agent.stream(request.message, thread_id=request.thread_id):
                yield {"event": event["type"], "data": json.dumps(event)}
        except Exception as exc:
            yield {"event": "error", "data": json.dumps({"error": str(exc)})}

    return EventSourceResponse(event_generator())


@router.delete("/rag/index")
async def reset_rag_index() -> dict:
    _get_rag_pipeline().reset_index()
    return {"status": "index_reset"}
