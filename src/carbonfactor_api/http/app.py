"""FastAPI application factory for local adapter testing."""

from __future__ import annotations

from typing import Any, Callable

from carbonfactor_api.http.routes import register_routes

try:
    from fastapi import FastAPI
except ModuleNotFoundError:  # pragma: no cover - local fallback for offline test environments
    class FastAPI:  # type: ignore[override]
        def __init__(self, title: str, version: str):
            self.title = title
            self.version = version
            self._routes: dict[tuple[str, str], Callable[..., Any]] = {}

        def get(self, path: str) -> Callable[[Callable[..., Any]], Callable[..., Any]]:
            def decorator(func: Callable[..., Any]) -> Callable[..., Any]:
                self._routes[("GET", path)] = func
                return func

            return decorator

        def dispatch(self, method: str, path: str, **kwargs: Any) -> Any:
            handler = self._routes.get((method, path))
            if handler is not None:
                return handler(**kwargs)
            for (registered_method, registered_path), registered_handler in self._routes.items():
                if registered_method != method:
                    continue
                if "{" not in registered_path:
                    continue
                prefix, param_spec = registered_path.split("{", 1)
                param_name = param_spec.rstrip("}")
                if not path.startswith(prefix):
                    continue
                route_value = path[len(prefix):]
                if "/" in route_value:
                    continue
                merged_kwargs = dict(kwargs)
                merged_kwargs[param_name] = route_value
                return registered_handler(**merged_kwargs)
            raise KeyError((method, path))


def create_app() -> FastAPI:
    app = FastAPI(title="CarbonFactor API", version="0.1.0")

    @app.get("/health")
    def health() -> dict[str, str]:
        return {"status": "ok", "adapter": "fastapi"}

    register_routes(app)
    return app
