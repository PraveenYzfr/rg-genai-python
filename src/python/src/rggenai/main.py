"""FastAPI application entry point."""

import uvicorn
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from rggenai import __version__
from rggenai.api.routes import router
from rggenai.config import get_settings
from rggenai.logging_config import setup_logging


def create_app() -> FastAPI:
    setup_logging()

    app = FastAPI(
        title="rgGenAI Python",
        description="Enterprise GenAI platform with RAG, LangChain, LangGraph, and MCP",
        version=__version__,
    )

    app.add_middleware(
        CORSMiddleware,
        allow_origins=["*"],
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )

    app.include_router(router)

    @app.get("/")
    async def root() -> dict:
        return {
            "name": "rgGenAI Python",
            "version": __version__,
            "docs": "/docs",
            "health": "/api/health",
        }

    return app


app = create_app()


def run() -> None:
    settings = get_settings()
    uvicorn.run(
        "rggenai.main:app",
        host=settings.api_host,
        port=settings.api_port,
        reload=settings.api_reload,
    )


if __name__ == "__main__":
    run()
