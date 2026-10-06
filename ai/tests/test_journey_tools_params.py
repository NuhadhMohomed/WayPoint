"""Tests for journey tool query parameter alignment with RouteController."""

import pytest
from unittest.mock import AsyncMock, patch


@pytest.mark.asyncio
async def test_search_routes_sends_correct_query_params():
    """Verify search_routes passes 'origin' and 'destination', not 'originCity'/'destinationCity'."""
    with patch("tools.journey_tools.make_tool_request", new_callable=AsyncMock) as mock_req:
        mock_req.return_value = []
        from tools.journey_tools import search_routes

        await search_routes.ainvoke(
            {"origin_city": "Colombo", "destination_city": "Ella"}
        )
        mock_req.assert_called_once()
        call_args = mock_req.call_args
        # params is the keyword arg or positional arg
        params = call_args.kwargs.get("params") or call_args[0][2]
        assert "origin" in params, f"Expected 'origin' key, got {list(params.keys())}"
        assert "destination" in params, f"Expected 'destination' key, got {list(params.keys())}"
        assert "originCity" not in params, "Should not use 'originCity'"
        assert "destinationCity" not in params, "Should not use 'destinationCity'"
        assert params["origin"] == "Colombo"
        assert params["destination"] == "Ella"
