"""RAG pipeline orchestration."""

import shutil
from pathlib import Path
from uuid import uuid4

from rggenai.config import get_settings
from rggenai.rag.ingestion import DocumentIngestor
from rggenai.rag.vectorstore import get_vector_store


class RagPipeline:
    def __init__(self) -> None:
        self.settings = get_settings()
        self.ingestor = DocumentIngestor(self.settings)
        self.vector_store = get_vector_store()
        self.settings.upload_dir.mkdir(parents=True, exist_ok=True)

    def ingest_upload(self, filename: str, content: bytes) -> dict:
        doc_id = str(uuid4())
        dest = self.settings.upload_dir / f"{doc_id}_{filename}"
        dest.write_bytes(content)

        chunks = self.ingestor.ingest_file(dest)
        ids = self.vector_store.add_documents(chunks)

        return {
            "document_id": doc_id,
            "filename": filename,
            "chunks_created": len(chunks),
            "chunk_ids": ids,
            "stored_path": str(dest),
        }

    def ingest_path(self, file_path: Path) -> dict:
        chunks = self.ingestor.ingest_file(file_path)
        ids = self.vector_store.add_documents(chunks)
        return {
            "filename": file_path.name,
            "chunks_created": len(chunks),
            "chunk_ids": ids,
        }

    def reset_index(self) -> None:
        self.vector_store.delete_collection()
        if self.settings.upload_dir.exists():
            shutil.rmtree(self.settings.upload_dir)
            self.settings.upload_dir.mkdir(parents=True, exist_ok=True)
