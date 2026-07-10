"""RAG retrieval and answer generation."""

from dataclasses import dataclass, field

from langchain_core.documents import Document
from langchain_core.prompts import ChatPromptTemplate

from rggenai.config import Settings, get_settings
from rggenai.llm.factory import LLMFactory, get_llm_factory
from rggenai.rag.vectorstore import VectorStoreManager, get_vector_store

RAG_SYSTEM_PROMPT = """You are a precise enterprise assistant. Answer using ONLY the provided context.
If the context is insufficient, say you don't have enough information — do not hallucinate.
Always cite sources using [source: filename] notation when referencing context."""


@dataclass
class RagCitation:
    source: str
    content_preview: str
    score: float | None = None


@dataclass
class RagSearchResult:
    query: str
    answer: str
    citations: list[RagCitation] = field(default_factory=list)
    chunks_retrieved: int = 0


@dataclass
class RagContextResult:
    query: str
    documents: list[Document]
    citations: list[RagCitation]


class RagService:
    def __init__(
        self,
        settings: Settings | None = None,
        vector_store: VectorStoreManager | None = None,
        llm_factory: LLMFactory | None = None,
    ) -> None:
        self.settings = settings or get_settings()
        self.vector_store = vector_store or get_vector_store()
        self.llm_factory = llm_factory or get_llm_factory()
        self._prompt = ChatPromptTemplate.from_messages(
            [
                ("system", RAG_SYSTEM_PROMPT),
                (
                    "human",
                    "Context:\n{context}\n\nQuestion: {question}\n\nAnswer:",
                ),
            ]
        )

    def retrieve(self, query: str, top_k: int | None = None) -> RagContextResult:
        results = self.vector_store.similarity_search_with_score(
            query, k=top_k or self.settings.rag_top_k
        )
        citations = [
            RagCitation(
                source=doc.metadata.get("source_file", "unknown"),
                content_preview=doc.page_content[:300],
                score=round(score, 4),
            )
            for doc, score in results
        ]
        return RagContextResult(
            query=query,
            documents=[doc for doc, _ in results],
            citations=citations,
        )

    def _format_context(self, documents: list[Document]) -> str:
        parts = []
        for i, doc in enumerate(documents, 1):
            source = doc.metadata.get("source_file", "unknown")
            parts.append(f"[{i}] (source: {source})\n{doc.page_content}")
        return "\n\n".join(parts)

    async def query(self, question: str, top_k: int | None = None) -> RagSearchResult:
        context_result = self.retrieve(question, top_k=top_k)
        if not context_result.documents:
            return RagSearchResult(
                query=question,
                answer="No relevant documents found in the knowledge base.",
                citations=[],
                chunks_retrieved=0,
            )

        context = self._format_context(context_result.documents)
        llm = self.llm_factory.create_chat_model()
        messages = self._prompt.format_messages(context=context, question=question)
        response = await llm.ainvoke(messages)

        return RagSearchResult(
            query=question,
            answer=response.content if isinstance(response.content, str) else str(response.content),
            citations=context_result.citations,
            chunks_retrieved=len(context_result.documents),
        )

    def query_sync(self, question: str, top_k: int | None = None) -> RagSearchResult:
        context_result = self.retrieve(question, top_k=top_k)
        if not context_result.documents:
            return RagSearchResult(
                query=question,
                answer="No relevant documents found in the knowledge base.",
                citations=[],
                chunks_retrieved=0,
            )

        context = self._format_context(context_result.documents)
        llm = self.llm_factory.create_chat_model()
        messages = self._prompt.format_messages(context=context, question=question)
        response = llm.invoke(messages)

        return RagSearchResult(
            query=question,
            answer=response.content if isinstance(response.content, str) else str(response.content),
            citations=context_result.citations,
            chunks_retrieved=len(context_result.documents),
        )
