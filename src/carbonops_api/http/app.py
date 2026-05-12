"""FastAPI application factory for local adapter testing."""

from fastapi import FastAPI

from carbonops_api.http.routes import register_routes


def create_app() -> FastAPI:
    app = FastAPI(title="CarbonFactor API", version="0.1.0")

    @app.get("/health")
    def health() -> dict[str, str]:
        return {"status": "ok", "adapter": "fastapi"}

    register_routes(app)
    return app
