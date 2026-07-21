"""Tests for multi-provider LLM support."""

import pytest

from rggenai.config import Settings
from rggenai.llm.providers import (
    LLMProvider,
    is_chat_provider_configured,
    is_embedding_provider_configured,
    list_provider_status,
    resolve_chat_provider,
    resolve_embedding_provider,
)


class TestProviderConfig:
    def test_openai_chat_configured(self) -> None:
        s = Settings(OPENAI_API_KEY="sk-test")
        assert is_chat_provider_configured(s, LLMProvider.OPENAI) is True

    def test_groq_chat_configured(self) -> None:
        s = Settings(GROQ_API_KEY="gsk-test")
        assert is_chat_provider_configured(s, LLMProvider.GROQ) is True

    def test_gemini_chat_configured(self) -> None:
        s = Settings(GOOGLE_API_KEY="AIza-test")
        assert is_chat_provider_configured(s, LLMProvider.GEMINI) is True

    def test_ollama_chat_configured(self) -> None:
        s = Settings(
            OLLAMA_ENABLED=True,
            OLLAMA_BASE_URL="http://localhost:11434",
            OLLAMA_MODEL="llama3.2",
        )
        assert is_chat_provider_configured(s, LLMProvider.OLLAMA) is True

    def test_groq_embeddings_not_supported(self) -> None:
        s = Settings(GROQ_API_KEY="gsk-test")
        assert is_embedding_provider_configured(s, LLMProvider.GROQ) is False

    def test_resolve_chat_provider_explicit(self) -> None:
        s = Settings(GROQ_API_KEY="gsk-test")
        assert resolve_chat_provider(s, "groq") == LLMProvider.GROQ

    def test_resolve_chat_provider_fallback(self) -> None:
        s = Settings(
            DEFAULT_LLM_PROVIDER="openai",
            GROQ_API_KEY="gsk-test",
        )
        assert resolve_chat_provider(s) == LLMProvider.GROQ

    def test_resolve_embedding_provider_ollama(self) -> None:
        s = Settings(
            DEFAULT_EMBEDDING_PROVIDER="ollama",
            OLLAMA_ENABLED=True,
            OLLAMA_BASE_URL="http://localhost:11434",
            OLLAMA_EMBEDDING_MODEL="nomic-embed-text",
        )
        assert resolve_embedding_provider(s) == LLMProvider.OLLAMA

    def test_resolve_chat_provider_raises_when_none_configured(self) -> None:
        s = Settings()
        with pytest.raises(ValueError, match="No LLM provider"):
            resolve_chat_provider(s)

    def test_list_provider_status(self) -> None:
        s = Settings(OPENAI_API_KEY="sk-test", GROQ_API_KEY="gsk-test")
        status = list_provider_status(s)
        assert len(status) == 4
        openai = next(p for p in status if p["id"] == "openai")
        assert openai["chat_available"] is True
