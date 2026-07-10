"""MCP server exposing rgGenAI tools to external clients (Cursor, Claude Desktop, etc.)."""

import asyncio
from typing import Any

from mcp.server import Server
from mcp.server.stdio import stdio_server
from mcp.types import TextContent, Tool

from rggenai.agents.graph import get_research_agent
from rggenai.config import get_settings
from rggenai.rag.service import RagService

server = Server(get_settings().mcp_server_name)


def _get_rag_service() -> RagService:
    return RagService()


@server.list_tools()
async def list_tools() -> list[Tool]:
    return [
        Tool(
            name="rag_search",
            description="Search the document knowledge base and return relevant chunks with citations.",
            inputSchema={
                "type": "object",
                "properties": {
                    "query": {
                        "type": "string",
                        "description": "Search query for the knowledge base",
                    },
                    "top_k": {
                        "type": "integer",
                        "description": "Number of results to return (default 5)",
                        "default": 5,
                    },
                },
                "required": ["query"],
            },
        ),
        Tool(
            name="rag_query",
            description="Ask a question and get an LLM-generated answer grounded in the knowledge base.",
            inputSchema={
                "type": "object",
                "properties": {
                    "question": {
                        "type": "string",
                        "description": "Question to answer using RAG",
                    },
                },
                "required": ["question"],
            },
        ),
        Tool(
            name="agent_run",
            description="Run the LangGraph research agent with tool calling (RAG + utilities).",
            inputSchema={
                "type": "object",
                "properties": {
                    "message": {
                        "type": "string",
                        "description": "User message for the agent",
                    },
                    "thread_id": {
                        "type": "string",
                        "description": "Conversation thread ID for checkpointing",
                        "default": "mcp-default",
                    },
                },
                "required": ["message"],
            },
        ),
    ]


@server.call_tool()
async def call_tool(name: str, arguments: dict[str, Any]) -> list[TextContent]:
    if name == "rag_search":
        query = arguments["query"]
        top_k = arguments.get("top_k", 5)
        result = _get_rag_service().retrieve(query, top_k=top_k)
        lines = [f"Query: {query}", f"Results: {len(result.documents)}", ""]
        for i, (doc, citation) in enumerate(
            zip(result.documents, result.citations, strict=False), 1
        ):
            lines.append(f"--- Result {i} (source: {citation.source}, score: {citation.score}) ---")
            lines.append(doc.page_content[:800])
            lines.append("")
        return [TextContent(type="text", text="\n".join(lines))]

    if name == "rag_query":
        question = arguments["question"]
        result = await _get_rag_service().query(question)
        lines = [
            f"Question: {result.query}",
            f"Answer: {result.answer}",
            "",
            "Citations:",
        ]
        for c in result.citations:
            lines.append(f"  - {c.source} (score: {c.score}): {c.content_preview[:200]}...")
        return [TextContent(type="text", text="\n".join(lines))]

    if name == "agent_run":
        message = arguments["message"]
        thread_id = arguments.get("thread_id", "mcp-default")
        agent = get_research_agent()
        result = await agent.run(message, thread_id=thread_id)
        lines = [
            f"Thread: {result['thread_id']}",
            f"Iterations: {result['iterations']}",
            f"Answer: {result['answer']}",
            "",
            "Steps:",
        ]
        for step in result.get("steps", []):
            lines.append(f"  - {step}")
        return [TextContent(type="text", text="\n".join(lines))]

    raise ValueError(f"Unknown tool: {name}")


async def run_server() -> None:
    async with stdio_server() as (read_stream, write_stream):
        await server.run(read_stream, write_stream, server.create_initialization_options())


def main() -> None:
    asyncio.run(run_server())


if __name__ == "__main__":
    main()
