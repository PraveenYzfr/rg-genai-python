"""Tests for rgGenAI Python platform."""

from unittest.mock import MagicMock

import pytest
from langchain_core.documents import Document

from rggenai.config import Settings
from rggenai.rag.ingestion import DocumentIngestor


@pytest.fixture
def settings() -> Settings:
    return Settings(
        OPENAI_API_KEY="test-key",
        RAG_CHUNK_SIZE=200,
        RAG_CHUNK_OVERLAP=50,
    )


@pytest.fixture
def ingestor(settings: Settings) -> DocumentIngestor:
    return DocumentIngestor(settings)


class TestDocumentIngestor:
    def test_chunk_documents(self, ingestor: DocumentIngestor) -> None:
        docs = [
            Document(
                page_content="A" * 500 + "\n\n" + "B" * 500,
                metadata={"source_file": "test.txt"},
            )
        ]
        chunks = ingestor.chunk_documents(docs)
        assert len(chunks) >= 2
        assert all("source_file" in c.metadata for c in chunks)

    def test_chunk_preserves_metadata(self, ingestor: DocumentIngestor) -> None:
        docs = [Document(page_content="Short text.", metadata={"source_file": "a.md"})]
        chunks = ingestor.chunk_documents(docs)
        assert chunks[0].metadata["source_file"] == "a.md"


class TestRagService:
    def test_format_context_empty(self, settings: Settings) -> None:
        from rggenai.rag.service import RagService

        service = RagService(
            settings=settings,
            vector_store=MagicMock(),
            llm_factory=MagicMock(),
        )
        assert service._format_context([]) == ""

    def test_format_context_with_docs(self, settings: Settings) -> None:
        from rggenai.rag.service import RagService

        service = RagService(
            settings=settings,
            vector_store=MagicMock(),
            llm_factory=MagicMock(),
        )
        docs = [
            Document(page_content="Hello world", metadata={"source_file": "doc.pdf"}),
        ]
        context = service._format_context(docs)
        assert "doc.pdf" in context
        assert "Hello world" in context


class TestSchemas:
    def test_health_response(self) -> None:
        from rggenai.api.schemas import HealthResponse

        resp = HealthResponse(status="healthy", version="0.1.0", components={})
        assert resp.status == "healthy"

    def test_agent_run_request_defaults(self) -> None:
        from rggenai.api.schemas import AgentRunRequest

        req = AgentRunRequest(message="hello")
        assert req.thread_id == "default"


class TestAgentState:
    def test_agent_state_typed_dict(self) -> None:
        from langchain_core.messages import HumanMessage

        from rggenai.agents.state import AgentState

        state: AgentState = {
            "messages": [HumanMessage(content="test")],
            "iteration": 0,
            "final_answer": None,
        }
        assert state["iteration"] == 0


class TestConfig:
    def test_settings_defaults(self) -> None:
        s = Settings()
        assert s.openai_model == "gpt-4o-mini"
        assert s.rag_top_k == 5

    def test_use_remote_chroma(self) -> None:
        s = Settings(CHROMA_HOST="localhost")
        assert s.use_remote_chroma is True

        s2 = Settings()
        assert s2.use_remote_chroma is False
