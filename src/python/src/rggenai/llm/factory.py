"""LLM provider factory using LangChain."""

from functools import lru_cache

from langchain_core.language_models.chat_models import BaseChatModel
from langchain_openai import ChatOpenAI, OpenAIEmbeddings

from rggenai.config import Settings, get_settings


class LLMFactory:
    def __init__(self, settings: Settings | None = None) -> None:
        self.settings = settings or get_settings()

    def create_chat_model(self, temperature: float = 0.2) -> BaseChatModel:
        if not self.settings.openai_api_key:
            raise ValueError(
                "OPENAI_API_KEY is required. Copy .env.example to .env and set your key."
            )
        return ChatOpenAI(
            model=self.settings.openai_model,
            api_key=self.settings.openai_api_key,
            temperature=temperature,
            streaming=True,
        )

    def create_embeddings(self) -> OpenAIEmbeddings:
        if not self.settings.openai_api_key:
            raise ValueError("OPENAI_API_KEY is required for embeddings.")
        return OpenAIEmbeddings(
            model=self.settings.openai_embedding_model,
            api_key=self.settings.openai_api_key,
        )


@lru_cache
def get_llm_factory() -> LLMFactory:
    return LLMFactory()
