using System.Net;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Integration;

public class AppointmentsApiIntegrationTests
    : IClassFixture<ApiIntegrationTestFactory>
{
    private readonly HttpClient _client;

    public AppointmentsApiIntegrationTests(ApiIntegrationTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAppointments_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/appointments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAppointment_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/appointments/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDoctorAppointments_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(
            "/api/appointments/doctor/1?fromUtc=2026-01-01T00:00:00Z&toUtc=2026-12-31T23:59:59Z");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPatientAppointments_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(
            "/api/appointments/patient/1?fromUtc=2026-01-01T00:00:00Z&toUtc=2026-12-31T23:59:59Z");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CheckAvailability_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(
            "/api/appointments/availability?doctorId=1&appointmentDateTimeUtc=2026-12-01T10:00:00Z");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAppointment_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync(
            "/api/appointments",
            new StringContent(
                """
                {
                    "patientId": 1,
                    "doctorId": 1,
                    "appointmentDateTime": "2026-12-01T10:00:00Z",
                    "status": "Scheduled"
                }
                """,
                System.Text.Encoding.UTF8,
                "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}