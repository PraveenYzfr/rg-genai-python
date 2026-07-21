"""Multi-provider LLM factory using LangChain."""

from functools import lru_cache

from langchain_core.embeddings import Embeddings
from langchain_core.language_models.chat_models import BaseChatModel

from rggenai.config import Settings, get_settings
from rggenai.llm.providers import (
    LLMProvider,
    resolve_chat_provider,
    resolve_embedding_provider,
)


class LLMFactory:
    def __init__(self, settings: Settings | None = None) -> None:
        self.settings = settings or get_settings()

    def create_chat_model(
        self,
        temperature: float = 0.2,
        provider: str | None = None,
    ) -> BaseChatModel:
        resolved = resolve_chat_provider(self.settings, provider)
        return self._build_chat_model(resolved, temperature)

    def create_embeddings(self, provider: str | None = None) -> Embeddings:
        resolved = resolve_embedding_provider(self.settings, provider)
        return self._build_embeddings(resolved)

    def _build_chat_model(self, provider: LLMProvider, temperature: float) -> BaseChatModel:
        if provider == LLMProvider.OPENAI:
            from langchain_openai import ChatOpenAI

            return ChatOpenAI(
                model=self.settings.openai_model,
                api_key=self.settings.openai_api_key,
                temperature=temperature,
                streaming=True,
            )

        if provider == LLMProvider.GROQ:
            from langchain_groq import ChatGroq

            return ChatGroq(
                model=self.settings.groq_model,
                api_key=self.settings.groq_api_key,
                temperature=temperature,
                streaming=True,
            )

        if provider == LLMProvider.GEMINI:
            from langchain_google_genai import ChatGoogleGenerativeAI

            return ChatGoogleGenerativeAI(
                model=self.settings.gemini_model,
                google_api_key=self.settings.google_api_key,
                temperature=temperature,
                streaming=True,
            )

        if provider == LLMProvider.OLLAMA:
            from langchain_ollama import ChatOllama

            return ChatOllama(
                model=self.settings.ollama_model,
                base_url=self.settings.ollama_base_url,
                temperature=temperature,
            )

        raise ValueError(f"Unsupported chat provider: {provider}")

    def _build_embeddings(self, provider: LLMProvider) -> Embeddings:
        if provider == LLMProvider.OPENAI:
            from langchain_openai import OpenAIEmbeddings

            return OpenAIEmbeddings(
                model=self.settings.openai_embedding_model,
                api_key=self.settings.openai_api_key,
            )

        if provider == LLMProvider.GEMINI:
            from langchain_google_genai import GoogleGenerativeAIEmbeddings

            return GoogleGenerativeAIEmbeddings(
                model=self.settings.gemini_embedding_model,
                google_api_key=self.settings.google_api_key,
            )

        if provider == LLMProvider.OLLAMA:
            from langchain_ollama import OllamaEmbeddings

            return OllamaEmbeddings(
                model=self.settings.ollama_embedding_model,
                base_url=self.settings.ollama_base_url,
            )

        raise ValueError(f"Unsupported embedding provider: {provider}")


@lru_cache
def get_llm_factory() -> LLMFactory:
    return LLMFactory()
