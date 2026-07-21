"""Pydantic request/response schemas for the API."""

from typing import Literal

from pydantic import BaseModel, Field

ProviderChoice = Literal["openai", "groq", "gemini", "ollama"]


class HealthResponse(BaseModel):
    status: str
    version: str
    components: dict[str, str]
    default_llm_provider: str
    default_embedding_provider: str


class ProviderInfo(BaseModel):
    id: str
    name: str
    chat_available: bool
    embeddings_available: bool
    is_default_chat: bool
    is_default_embeddings: bool
    default_chat_model: str
    default_embedding_model: str | None = None


class ProvidersResponse(BaseModel):
    providers: list[ProviderInfo]
    usage_hint: str


class RagSearchRequest(BaseModel):
    query: str
    top_k: int | None = Field(default=None, ge=1, le=20)


class RagCitationResponse(BaseModel):
    source: str
    content_preview: str
    score: float | None = None


class RagSearchResponse(BaseModel):
    query: str
    documents: list[dict]
    citations: list[RagCitationResponse]


class RagQueryRequest(BaseModel):
    question: str
    top_k: int | None = Field(default=None, ge=1, le=20)
    provider: ProviderChoice | None = Field(
        default=None,
        description="LLM provider override: openai, groq, gemini, or ollama",
    )


class RagQueryResponse(BaseModel):
    query: str
    answer: str
    citations: list[RagCitationResponse]
    chunks_retrieved: int
    provider: str


class DocumentUploadResponse(BaseModel):
    document_id: str
    filename: str
    chunks_created: int
    chunk_ids: list[str]


class AgentRunRequest(BaseModel):
    message: str
    thread_id: str = "default"
    provider: ProviderChoice | None = Field(
        default=None,
        description="LLM provider override: openai, groq, gemini, or ollama",
    )


class AgentStepResponse(BaseModel):
    type: str
    tool: str | None = None
    args: dict | None = None


class AgentRunResponse(BaseModel):
    thread_id: str
    answer: str
    steps: list[dict]
    iterations: int
    provider: str


class ChatRequest(BaseModel):
    message: str
    thread_id: str = "default"
    stream: bool = False
    provider: ProviderChoice | None = None
