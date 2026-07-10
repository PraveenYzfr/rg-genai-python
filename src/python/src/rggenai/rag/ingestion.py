"""Document ingestion: parsing and chunking."""

from pathlib import Path

from langchain_community.document_loaders import PyPDFLoader, TextLoader
from langchain_core.documents import Document
from langchain_text_splitters import RecursiveCharacterTextSplitter

from rggenai.config import Settings, get_settings


class DocumentIngestor:
    def __init__(self, settings: Settings | None = None) -> None:
        self.settings = settings or get_settings()
        self.splitter = RecursiveCharacterTextSplitter(
            chunk_size=self.settings.rag_chunk_size,
            chunk_overlap=self.settings.rag_chunk_overlap,
            separators=["\n\n", "\n", ". ", " ", ""],
        )

    def load_file(self, file_path: Path) -> list[Document]:
        suffix = file_path.suffix.lower()
        if suffix == ".pdf":
            loader = PyPDFLoader(str(file_path))
        elif suffix in {".txt", ".md", ".markdown"}:
            loader = TextLoader(str(file_path), encoding="utf-8")
        else:
            raise ValueError(f"Unsupported file type: {suffix}")

        documents = loader.load()
        for doc in documents:
            doc.metadata["source_file"] = file_path.name
            doc.metadata["source_path"] = str(file_path)
        return documents

    def chunk_documents(self, documents: list[Document]) -> list[Document]:
        return self.splitter.split_documents(documents)

    def ingest_file(self, file_path: Path) -> list[Document]:
        documents = self.load_file(file_path)
        return self.chunk_documents(documents)
