"""LangGraph ReAct agent with RAG tools and checkpointing."""

from functools import lru_cache
from typing import Any, Literal

from langchain_core.messages import AIMessage, HumanMessage, SystemMessage
from langgraph.checkpoint.sqlite.aio import AsyncSqliteSaver
from langgraph.graph import END, StateGraph
from langgraph.prebuilt import ToolNode

from rggenai.agents.state import AgentState
from rggenai.agents.tools import create_rag_tools
from rggenai.config import get_settings
from rggenai.llm.factory import get_llm_factory
from rggenai.logging_config import get_logger

logger = get_logger(__name__)

AGENT_SYSTEM_PROMPT = """You are rgGenAI, an enterprise AI assistant with access to tools.
- Use search_knowledge_base for questions about uploaded documents.
- Use get_current_time when the user needs current date/time.
- Be concise, accurate, and cite sources when using retrieved knowledge.
- If you cannot answer from available tools, say so clearly."""


class ResearchAgent:
    """ReAct-style LangGraph agent with tool calling and persistent checkpoints."""

    MAX_ITERATIONS = 8

    def __init__(self) -> None:
        self.settings = get_settings()
        self.tools = create_rag_tools()
        self.llm = get_llm_factory().create_chat_model().bind_tools(self.tools)
        self.tool_node = ToolNode(self.tools)
        self._graph = None
        self._checkpointer: AsyncSqliteSaver | None = None

    async def _ensure_checkpointer(self) -> AsyncSqliteSaver:
        if self._checkpointer is None:
            self.settings.checkpoint_db_path.parent.mkdir(parents=True, exist_ok=True)
            self._checkpointer = AsyncSqliteSaver.from_conn_string(
                str(self.settings.checkpoint_db_path)
            )
            await self._checkpointer.setup()
        return self._checkpointer

    def _should_continue(self, state: AgentState) -> Literal["tools", "end"]:
        if state.get("iteration", 0) >= self.MAX_ITERATIONS:
            return "end"
        messages = state["messages"]
        last = messages[-1]
        if isinstance(last, AIMessage) and last.tool_calls:
            return "tools"
        return "end"

    async def _call_model(self, state: AgentState) -> dict[str, Any]:
        messages = state["messages"]
        if not messages or not isinstance(messages[0], SystemMessage):
            messages = [SystemMessage(content=AGENT_SYSTEM_PROMPT)] + list(messages)

        response = await self.llm.ainvoke(messages)
        iteration = state.get("iteration", 0) + 1

        final_answer = None
        if isinstance(response, AIMessage) and response.content and not response.tool_calls:
            final_answer = (
                response.content
                if isinstance(response.content, str)
                else str(response.content)
            )

        return {
            "messages": [response],
            "iteration": iteration,
            "final_answer": final_answer,
        }

    def _build_graph(self) -> StateGraph:
        graph = StateGraph(AgentState)
        graph.add_node("agent", self._call_model)
        graph.add_node("tools", self.tool_node)
        graph.set_entry_point("agent")
        graph.add_conditional_edges(
            "agent",
            self._should_continue,
            {"tools": "tools", "end": END},
        )
        graph.add_edge("tools", "agent")
        return graph

    async def get_compiled_graph(self):
        if self._graph is None:
            checkpointer = await self._ensure_checkpointer()
            graph = self._build_graph()
            self._graph = graph.compile(checkpointer=checkpointer)
        return self._graph

    async def run(
        self,
        message: str,
        thread_id: str = "default",
    ) -> dict[str, Any]:
        graph = await self.get_compiled_graph()
        config = {"configurable": {"thread_id": thread_id}}

        initial_state: AgentState = {
            "messages": [HumanMessage(content=message)],
            "iteration": 0,
            "final_answer": None,
        }

        logger.info("agent_run_start", thread_id=thread_id, message=message[:100])
        result = await graph.ainvoke(initial_state, config=config)

        final = result.get("final_answer")
        if not final:
            for msg in reversed(result["messages"]):
                if isinstance(msg, AIMessage) and msg.content:
                    final = msg.content if isinstance(msg.content, str) else str(msg.content)
                    break

        steps = []
        for msg in result["messages"]:
            if isinstance(msg, AIMessage) and msg.tool_calls:
                for tc in msg.tool_calls:
                    steps.append({"type": "tool_call", "tool": tc["name"], "args": tc["args"]})
            elif hasattr(msg, "name") and msg.name:
                steps.append({"type": "tool_result", "tool": msg.name})

        return {
            "thread_id": thread_id,
            "answer": final or "No response generated.",
            "steps": steps,
            "iterations": result.get("iteration", 0),
        }

    async def stream(
        self,
        message: str,
        thread_id: str = "default",
    ):
        graph = await self.get_compiled_graph()
        config = {"configurable": {"thread_id": thread_id}}
        initial_state: AgentState = {
            "messages": [HumanMessage(content=message)],
            "iteration": 0,
            "final_answer": None,
        }

        async for event in graph.astream_events(initial_state, config=config, version="v2"):
            kind = event.get("event")
            if kind == "on_chat_model_stream":
                chunk = event["data"].get("chunk")
                if chunk and hasattr(chunk, "content") and chunk.content:
                    yield {"type": "token", "content": chunk.content}
            elif kind == "on_tool_start":
                yield {
                    "type": "tool_start",
                    "tool": event.get("name", "unknown"),
                    "input": event["data"].get("input"),
                }
            elif kind == "on_tool_end":
                yield {
                    "type": "tool_end",
                    "tool": event.get("name", "unknown"),
                    "output": str(event["data"].get("output", ""))[:500],
                }


@lru_cache
def get_research_agent() -> ResearchAgent:
    return ResearchAgent()
