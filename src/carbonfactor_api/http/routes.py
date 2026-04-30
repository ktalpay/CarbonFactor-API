"""Route wiring for the thin HTTP adapter."""

from fastapi import Request, Response

from carbonfactor_api.http.query import build_factor_query, unsupported_query_envelope, unsupported_query_keys
from carbonfactor_api.transport.handlers import handle_get_factor, handle_list_factors


def register_routes(app) -> None:
    @app.get("/factors")
    def list_factors(
        request: Request,
        response: Response,
        category: str | None = None,
        activity: str | None = None,
        region: str | None = None,
        year: int | None = None,
    ) -> dict:
        query = build_factor_query(category=category, activity=activity, region=region, year=year)
        unsupported_keys = unsupported_query_keys(request)
        if unsupported_keys:
            envelope = unsupported_query_envelope(unsupported_keys).to_dict()
            response.status_code = envelope["status"]
            return envelope

        envelope = handle_list_factors(query).to_dict()
        response.status_code = envelope["status"]
        return envelope

    @app.get("/factors/{factor_id}")
    def get_factor(factor_id: str, response: Response) -> dict:
        envelope = handle_get_factor(factor_id).to_dict()
        response.status_code = envelope["status"]
        return envelope
