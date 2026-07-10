"""LangChain tools exposed to agents and MCP."""

from datetime import UTC

from langchain_core.tools import tool

from rggenai.rag.service import RagService


def create_rag_tools(rag_service: RagService | None = None) -> list:
    _service = rag_service

    def _get_service() -> RagService:
        nonlocal _service
        if _service is None:
            _service = RagService()
        return _service

    @tool
    def search_knowledge_base(query: str) -> str:
        """Search the document knowledge base for relevant information.
        Use this when the user asks about uploaded documents or internal knowledge."""
        result = _get_service().retrieve(query)
        if not result.documents:
            return "No relevant documents found."
        parts = []
        for i, (doc, citation) in enumerate(
            zip(result.documents, result.citations, strict=False), 1
        ):
            parts.append(
                f"[{i}] Source: {citation.source} (score: {citation.score})\n"
                f"{doc.page_content[:500]}"
            )
        return "\n\n".join(parts)

    @tool
    def get_current_time() -> str:
        """Get the current UTC date and time. Use for time-sensitive queries."""
        from datetime import datetime

        return datetime.now(UTC).strftime("%Y-%m-%d %H:%M:%S UTC")

    return [search_knowledge_base, get_current_time]
