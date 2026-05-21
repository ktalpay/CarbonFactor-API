"""Route wiring for the thin HTTP adapter."""

from fastapi import Request, Response

from carbonops_api.http.query import build_factor_query, unsupported_query_envelope, unsupported_query_keys
from carbonops_api.contracts import (
    ParserCarbonFactorBatchImportRequest,
    ParserCarbonFactorImportItem,
    ParserSourceMetadataDto,
)
from carbonops_api.transport.handlers import handle_get_factor, handle_import_carbon_factors, handle_list_factors


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

    @app.post("/carbon-factors/import")
    def import_carbon_factors(payload: dict, response: Response) -> dict:
        request_payload = ParserCarbonFactorBatchImportRequest(
            contract_version=str(payload.get("contract_version", "")),
            batch_id=str(payload.get("batch_id", "")),
            source=ParserSourceMetadataDto(
                source_family=str(payload.get("source", {}).get("source_family", "")),
                source_provider=str(payload.get("source", {}).get("source_provider", "")),
            ),
            factors=[
                ParserCarbonFactorImportItem(
                    external_factor_id=str(item.get("external_factor_id", "")),
                    source_family=str(item.get("source_family", "")),
                    source_provider=str(item.get("source_provider", "")),
                    category=str(item.get("category", "")),
                    activity=str(item.get("activity", "")),
                    factor_value=item.get("factor_value"),
                    factor_unit=str(item.get("factor_unit", "")),
                )
                for item in payload.get("factors", [])
            ],
        )
        envelope = handle_import_carbon_factors(request_payload).to_dict()
        response.status_code = envelope["status"]
        return envelope
