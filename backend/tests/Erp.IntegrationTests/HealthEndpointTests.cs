using System.Net;
using System.Net.Http.Json;
using Erp.Api.Controllers;
using Erp.Shared.Correlation;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Erp.IntegrationTests;

/// <summary>
/// Boots the real API in-memory (no database needed — the health endpoint is
/// unauthenticated and DB-free) to verify the Phase 0 plumbing: the endpoint
/// responds and the correlation-ID middleware echoes a correlation header.
/// </summary>
public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_returns_200_and_healthy_payload()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("healthy", body!.Status);
        Assert.Equal("Erp.Api", body.Service);
    }

    [Fact]
    public async Task Response_always_carries_a_correlation_id_header()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/health");

        Assert.True(response.Headers.Contains(CorrelationConstants.HeaderName));
        var value = response.Headers.GetValues(CorrelationConstants.HeaderName).Single();
        Assert.False(string.IsNullOrWhiteSpace(value));
    }

    [Fact]
    public async Task Inbound_correlation_id_is_echoed_back()
    {
        var client = _factory.CreateClient();
        const string correlationId = "test-correlation-123";

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/health");
        request.Headers.Add(CorrelationConstants.HeaderName, correlationId);

        var response = await client.SendAsync(request);

        var echoed = response.Headers.GetValues(CorrelationConstants.HeaderName).Single();
        Assert.Equal(correlationId, echoed);
    }
}
