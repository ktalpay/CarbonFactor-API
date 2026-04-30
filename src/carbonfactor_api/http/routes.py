"""Route wiring for the thin HTTP adapter."""

from __future__ import annotations

from carbonfactor_api.transport.handlers import handle_get_factor, handle_list_factors


def register_routes(app) -> None:
    @app.get("/factors")
    def list_factors(
        category: str | None = None,
        activity: str | None = None,
        region: str | None = None,
        year: int | None = None,
        **extra_filters: str,
    ) -> dict:
        query = {
            key: value
            for key, value in {
                "category": category,
                "activity": activity,
                "region": region,
                "year": year,
            }.items()
            if value is not None
        }
        query.update(extra_filters)
        return handle_list_factors(query).to_dict()

    @app.get("/factors/{factor_id}")
    def get_factor(factor_id: str) -> dict:
        return handle_get_factor(factor_id).to_dict()
