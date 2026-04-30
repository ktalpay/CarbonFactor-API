"""Route wiring for the thin HTTP adapter."""

from fastapi import Response

from carbonfactor_api.transport.handlers import handle_get_factor, handle_list_factors


def register_routes(app) -> None:
    @app.get("/factors")
    def list_factors(
        response: Response,
        category: str | None = None,
        activity: str | None = None,
        region: str | None = None,
        year: int | None = None,
        unsupported: str | None = None,
    ) -> dict:
        query = {
            key: value
            for key, value in {
                "category": category,
                "activity": activity,
                "region": region,
                "year": year,
                "unsupported": unsupported,
            }.items()
            if value is not None
        }
        envelope = handle_list_factors(query).to_dict()
        response.status_code = envelope["status"]
        return envelope

    @app.get("/factors/{factor_id}")
    def get_factor(factor_id: str, response: Response) -> dict:
        envelope = handle_get_factor(factor_id).to_dict()
        response.status_code = envelope["status"]
        return envelope
