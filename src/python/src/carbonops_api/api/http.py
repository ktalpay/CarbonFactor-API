"""API boundary wrapper for the current FastAPI adapter."""

from carbonops_api.http.app import create_app

__all__ = ["create_app"]
