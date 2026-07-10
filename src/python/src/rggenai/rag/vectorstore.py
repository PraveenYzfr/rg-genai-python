"""ChromaDB vector store management."""

from functools import lru_cache
from uuid import uuid4

import chromadb
from chromadb.config import Settings as ChromaSettings
from langchain_chroma import Chroma
from langchain_core.documents import Document
from langchain_core.embeddings import Embeddings

from rggenai.config import Settings, get_settings
from rggenai.llm.factory import get_llm_factory


class VectorStoreManager:
    def __init__(
        self,
        settings: Settings | None = None,
        embeddings: Embeddings | None = None,
    ) -> None:
        self.settings = settings or get_settings()
        self.embeddings = embeddings or get_llm_factory().create_embeddings()
        self._store: Chroma | None = None

    def _get_client(self):
        if self.settings.use_remote_chroma:
            return chromadb.HttpClient(
                host=self.settings.chroma_host,
                port=self.settings.chroma_port,
            )
        self.settings.chroma_persist_dir.mkdir(parents=True, exist_ok=True)
        return chromadb.PersistentClient(
            path=str(self.settings.chroma_persist_dir),
            settings=ChromaSettings(anonymized_telemetry=False),
        )

    @property
    def store(self) -> Chroma:
        if self._store is None:
            self._store = Chroma(
                client=self._get_client(),
                collection_name=self.settings.chroma_collection_name,
                embedding_function=self.embeddings,
            )
        return self._store

    def add_documents(self, documents: list[Document]) -> list[str]:
        ids = [str(uuid4()) for _ in documents]
        self.store.add_documents(documents=documents, ids=ids)
        return ids

    def similarity_search(
        self, query: str, k: int | None = None
    ) -> list[Document]:
        return self.store.similarity_search(
            query, k=k or self.settings.rag_top_k
        )

    def similarity_search_with_score(
        self, query: str, k: int | None = None
    ) -> list[tuple[Document, float]]:
        return self.store.similarity_search_with_score(
            query, k=k or self.settings.rag_top_k
        )

    def delete_collection(self) -> None:
        client = self._get_client()
        try:
            client.delete_collection(self.settings.chroma_collection_name)
        except Exception:
            pass
        self._store = None

    def document_count(self) -> int:
        return self.store._collection.count()


@lru_cache
def get_vector_store() -> VectorStoreManager:
    return VectorStoreManager()
