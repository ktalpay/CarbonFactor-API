"""Synthetic, deterministic factor sample data."""

from carbonops_api.contracts import FactorDto


SAMPLE_FACTORS: tuple[FactorDto, ...] = (
    FactorDto(
        id="f-001",
        source="synthetic",
        category="electricity",
        activity="grid_consumption",
        factor_value=0.42,
        factor_unit="kgCO2e/kWh",
        region="US-AVG",
        year=2024,
        notes="Synthetic sample",
    ),
    FactorDto(
        id="f-002",
        source="synthetic",
        category="transport",
        activity="diesel_vehicle",
        factor_value=2.68,
        factor_unit="kgCO2e/liter",
        region="US",
        year=2023,
        notes="Synthetic sample",
    ),
    FactorDto(
        id="f-003",
        source="synthetic",
        category="electricity",
        activity="market_based",
        factor_value=0.31,
        factor_unit="kgCO2e/kWh",
        region="US-WEST",
        year=2024,
        notes="Synthetic sample",
    ),
)
