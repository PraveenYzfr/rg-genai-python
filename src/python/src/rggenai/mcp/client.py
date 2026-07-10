"""MCP client utilities for connecting to external MCP servers."""

from contextlib import asynccontextmanager
from typing import Any

import httpx
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

from rggenai.logging_config import get_logger

logger = get_logger(__name__)


@asynccontextmanager
async def connect_stdio_mcp(command: str, args: list[str] | None = None):
    """Connect to an external MCP server via stdio transport."""
    server_params = StdioServerParameters(command=command, args=args or [])
    async with stdio_client(server_params) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()
            yield session


async def list_external_tools(session: ClientSession) -> list[dict[str, Any]]:
    tools = await session.list_tools()
    return [
        {"name": t.name, "description": t.description, "schema": t.inputSchema}
        for t in tools.tools
    ]


async def call_external_tool(
    session: ClientSession, name: str, arguments: dict[str, Any]
) -> str:
    result = await session.call_tool(name, arguments)
    parts = []
    for content in result.content:
        if hasattr(content, "text"):
            parts.append(content.text)
    return "\n".join(parts)


class HttpMcpBridge:
    """Lightweight HTTP bridge for MCP-style tool invocation via REST."""

    def __init__(self, base_url: str) -> None:
        self.base_url = base_url.rstrip("/")

    async def health(self) -> dict[str, Any]:
        async with httpx.AsyncClient() as client:
            resp = await client.get(f"{self.base_url}/api/health")
            resp.raise_for_status()
            return resp.json()

    async def rag_search(self, query: str, top_k: int = 5) -> dict[str, Any]:
        async with httpx.AsyncClient(timeout=60.0) as client:
            resp = await client.post(
                f"{self.base_url}/api/rag/search",
                json={"query": query, "top_k": top_k},
            )
            resp.raise_for_status()
            return resp.json()

    async def agent_run(self, message: str, thread_id: str = "default") -> dict[str, Any]:
        async with httpx.AsyncClient(timeout=120.0) as client:
            resp = await client.post(
                f"{self.base_url}/api/agents/run",
                json={"message": message, "thread_id": thread_id},
            )
            resp.raise_for_status()
            return resp.json()
