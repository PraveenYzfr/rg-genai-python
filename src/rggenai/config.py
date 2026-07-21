"""Application configuration via environment variables."""
"""ATest Chckin-in"""

from functools import lru_cache
from pathlib import Path

from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",
    )

    # Provider selection (openai | groq | gemini | ollama)
    default_llm_provider: str = Field(default="openai", alias="DEFAULT_LLM_PROVIDER")
    default_embedding_provider: str = Field(default="openai", alias="DEFAULT_EMBEDDING_PROVIDER")

    # OpenAI
    openai_api_key: str = Field(default="", alias="OPENAI_API_KEY")
    openai_model: str = Field(default="gpt-4o-mini", alias="OPENAI_MODEL")
    openai_embedding_model: str = Field(
        default="text-embedding-3-small", alias="OPENAI_EMBEDDING_MODEL"
    )

    # Groq (free tier, very fast)
    groq_api_key: str = Field(default="", alias="GROQ_API_KEY")
    groq_model: str = Field(default="llama-3.3-70b-versatile", alias="GROQ_MODEL")

    # Google Gemini (free tier)
    google_api_key: str = Field(default="", alias="GOOGLE_API_KEY")
    gemini_model: str = Field(default="gemini-2.0-flash", alias="GEMINI_MODEL")
    gemini_embedding_model: str = Field(
        default="models/text-embedding-004", alias="GEMINI_EMBEDDING_MODEL"
    )

    # Ollama (local, free)
    ollama_enabled: bool = Field(default=False, alias="OLLAMA_ENABLED")
    ollama_base_url: str = Field(default="http://localhost:11434", alias="OLLAMA_BASE_URL")
    ollama_model: str = Field(default="llama3.2", alias="OLLAMA_MODEL")
    ollama_embedding_model: str = Field(default="nomic-embed-text", alias="OLLAMA_EMBEDDING_MODEL")

    # RAG
    chroma_persist_dir: Path = Field(
        default=Path("./data/chroma"), alias="CHROMA_PERSIST_DIR"
    )
    chroma_collection_name: str = Field(
        default="rggenai_docs", alias="CHROMA_COLLECTION_NAME"
    )
    chroma_host: str | None = Field(default=None, alias="CHROMA_HOST")
    chroma_port: int = Field(default=8000, alias="CHROMA_PORT")
    rag_chunk_size: int = Field(default=1000, alias="RAG_CHUNK_SIZE")
    rag_chunk_overlap: int = Field(default=200, alias="RAG_CHUNK_OVERLAP")
    rag_top_k: int = Field(default=5, alias="RAG_TOP_K")

    # API
    api_host: str = Field(default="0.0.0.0", alias="API_HOST")
    api_port: int = Field(default=8000, alias="API_PORT")
    api_reload: bool = Field(default=True, alias="API_RELOAD")
    log_level: str = Field(default="INFO", alias="LOG_LEVEL")

    # Paths
    upload_dir: Path = Field(default=Path("./uploads"))
    checkpoint_db_path: Path = Field(
        default=Path("./data/checkpoints.db"), alias="CHECKPOINT_DB_PATH"
    )

    # MCP
    mcp_server_name: str = Field(default="rggenai-mcp", alias="MCP_SERVER_NAME")

    @property
    def use_remote_chroma(self) -> bool:
        return self.chroma_host is not None


@lru_cache
def get_settings() -> Settings:
    return Settings()
