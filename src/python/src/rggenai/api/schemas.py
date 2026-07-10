"""Pydantic request/response schemas for the API."""

from pydantic import BaseModel, Field


class HealthResponse(BaseModel):
    status: str
    version: str
    components: dict[str, str]


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


class RagQueryResponse(BaseModel):
    query: str
    answer: str
    citations: list[RagCitationResponse]
    chunks_retrieved: int


class DocumentUploadResponse(BaseModel):
    document_id: str
    filename: str
    chunks_created: int
    chunk_ids: list[str]


class AgentRunRequest(BaseModel):
    message: str
    thread_id: str = "default"


class AgentStepResponse(BaseModel):
    type: str
    tool: str | None = None
    args: dict | None = None


class AgentRunResponse(BaseModel):
    thread_id: str
    answer: str
    steps: list[dict]
    iterations: int


class ChatRequest(BaseModel):
    message: str
    thread_id: str = "default"
    stream: bool = False
