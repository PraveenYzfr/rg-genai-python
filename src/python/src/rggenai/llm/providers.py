"""LLM provider definitions and availability checks."""

from enum import Enum

from rggenai.config import Settings


class LLMProvider(str, Enum):
    OPENAI = "openai"
    GROQ = "groq"
    GEMINI = "gemini"
    OLLAMA = "ollama"

    @classmethod
    def from_value(cls, value: str | None) -> "LLMProvider":
        if not value:
            raise ValueError("Provider value is required")
        normalized = value.strip().lower()
        try:
            return cls(normalized)
        except ValueError as exc:
            valid = ", ".join(p.value for p in cls)
            raise ValueError(f"Unknown provider '{value}'. Valid options: {valid}") from exc


PROVIDER_LABELS: dict[LLMProvider, str] = {
    LLMProvider.OPENAI: "OpenAI",
    LLMProvider.GROQ: "Groq (free tier, very fast)",
    LLMProvider.GEMINI: "Google Gemini (free tier)",
    LLMProvider.OLLAMA: "Ollama (local, free)",
}

PROVIDER_CHAT_MODEL_DEFAULTS: dict[LLMProvider, str] = {
    LLMProvider.OPENAI: "gpt-4o-mini",
    LLMProvider.GROQ: "llama-3.3-70b-versatile",
    LLMProvider.GEMINI: "gemini-2.0-flash",
    LLMProvider.OLLAMA: "llama3.2",
}

# Groq has no embedding API — excluded from embedding providers.
EMBEDDING_PROVIDERS = [LLMProvider.OPENAI, LLMProvider.GEMINI, LLMProvider.OLLAMA]

CHAT_FALLBACK_ORDER = [
    LLMProvider.OPENAI,
    LLMProvider.GROQ,
    LLMProvider.GEMINI,
    LLMProvider.OLLAMA,
]

EMBEDDING_FALLBACK_ORDER = [
    LLMProvider.OPENAI,
    LLMProvider.OLLAMA,
    LLMProvider.GEMINI,
]


def is_chat_provider_configured(settings: Settings, provider: LLMProvider) -> bool:
    if provider == LLMProvider.OPENAI:
        return bool(settings.openai_api_key)
    if provider == LLMProvider.GROQ:
        return bool(settings.groq_api_key)
    if provider == LLMProvider.GEMINI:
        return bool(settings.google_api_key)
    if provider == LLMProvider.OLLAMA:
        return bool(
            settings.ollama_enabled
            and settings.ollama_base_url
            and settings.ollama_model
        )
    return False


def is_embedding_provider_configured(settings: Settings, provider: LLMProvider) -> bool:
    if provider == LLMProvider.OPENAI:
        return bool(settings.openai_api_key)
    if provider == LLMProvider.GEMINI:
        return bool(settings.google_api_key)
    if provider == LLMProvider.OLLAMA:
        return bool(
            settings.ollama_enabled
            and settings.ollama_base_url
            and settings.ollama_embedding_model
        )
    return False


def resolve_chat_provider(settings: Settings, requested: str | None = None) -> LLMProvider:
    if requested:
        provider = LLMProvider.from_value(requested)
        if not is_chat_provider_configured(settings, provider):
            raise ValueError(
                f"Provider '{provider.value}' is not configured. "
                f"Check your .env file for the required API key or URL."
            )
        return provider

    default = LLMProvider.from_value(settings.default_llm_provider)
    if is_chat_provider_configured(settings, default):
        return default

    for provider in CHAT_FALLBACK_ORDER:
        if is_chat_provider_configured(settings, provider):
            return provider

    raise ValueError(
        "No LLM provider is configured. Set at least one of: "
        "OPENAI_API_KEY, GROQ_API_KEY, GOOGLE_API_KEY, or OLLAMA_BASE_URL."
    )


def resolve_embedding_provider(settings: Settings, requested: str | None = None) -> LLMProvider:
    if requested:
        provider = LLMProvider.from_value(requested)
        if provider == LLMProvider.GROQ:
            raise ValueError("Groq does not support embeddings. Use openai, gemini, or ollama.")
        if not is_embedding_provider_configured(settings, provider):
            raise ValueError(f"Embedding provider '{provider.value}' is not configured.")
        return provider

    default = LLMProvider.from_value(settings.default_embedding_provider)
    if is_embedding_provider_configured(settings, default):
        return default

    for provider in EMBEDDING_FALLBACK_ORDER:
        if is_embedding_provider_configured(settings, provider):
            return provider

    raise ValueError(
        "No embedding provider is configured. Set OPENAI_API_KEY, GOOGLE_API_KEY, "
        "or OLLAMA_BASE_URL with OLLAMA_EMBEDDING_MODEL."
    )


def list_provider_status(settings: Settings) -> list[dict]:
    providers = []
    for provider in LLMProvider:
        chat_ready = is_chat_provider_configured(settings, provider)
        embed_ready = is_embedding_provider_configured(settings, provider)
        providers.append(
            {
                "id": provider.value,
                "name": PROVIDER_LABELS[provider],
                "chat_available": chat_ready,
                "embeddings_available": embed_ready,
                "is_default_chat": provider.value == settings.default_llm_provider.lower(),
                "is_default_embeddings": (
                    provider.value == settings.default_embedding_provider.lower()
                ),
                "default_chat_model": _chat_model_for(settings, provider),
                "default_embedding_model": _embedding_model_for(settings, provider),
            }
        )
    return providers


def _chat_model_for(settings: Settings, provider: LLMProvider) -> str:
    mapping = {
        LLMProvider.OPENAI: settings.openai_model,
        LLMProvider.GROQ: settings.groq_model,
        LLMProvider.GEMINI: settings.gemini_model,
        LLMProvider.OLLAMA: settings.ollama_model,
    }
    return mapping[provider]


def _embedding_model_for(settings: Settings, provider: LLMProvider) -> str | None:
    mapping = {
        LLMProvider.OPENAI: settings.openai_embedding_model,
        LLMProvider.GEMINI: settings.gemini_embedding_model,
        LLMProvider.OLLAMA: settings.ollama_embedding_model,
    }
    return mapping.get(provider)
