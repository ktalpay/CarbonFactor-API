from fastapi.testclient import TestClient

from carbonfactor_api.http.app import create_app


def test_openapi_metadata_is_deterministic() -> None:
    client = TestClient(create_app())
    spec = client.get('/openapi.json').json()
    assert spec['info']['title'] == 'CarbonFactor API'
    assert spec['info']['version'] == '0.1.0'


def test_openapi_contains_expected_paths() -> None:
    client = TestClient(create_app())
    spec = client.get('/openapi.json').json()
    assert '/health' in spec['paths']
    assert '/factors' in spec['paths']
    assert '/factors/{factor_id}' in spec['paths']


def test_openapi_lists_supported_factors_query_params() -> None:
    client = TestClient(create_app())
    spec = client.get('/openapi.json').json()
    params = spec['paths']['/factors']['get']['parameters']
    param_names = {param['name'] for param in params}
    assert {'category', 'activity', 'region', 'year'}.issubset(param_names)
