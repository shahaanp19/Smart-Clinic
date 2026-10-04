using System.Net;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Integration;

public class PrescriptionsApiIntegrationTests
    : IClassFixture<ApiIntegrationTestFactory>
{
    private readonly HttpClient _client;

    public PrescriptionsApiIntegrationTests(ApiIntegrationTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPrescription_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/prescriptions/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetConsultationPrescriptions_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/prescriptions/consultation/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPatientPrescriptions_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/prescriptions/patient/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDoctorPrescriptions_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/prescriptions/doctor/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreatePrescription_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync(
            "/api/prescriptions",
            new StringContent(
                """
                {
                    "consultationId": 1,
                    "patientId": 1,
                    "doctorId": 1,
                    "medicationName": "Test Medication",
                    "dosage": "10 mg",
                    "frequency": "Once daily",
                    "duration": "7 days"
                }
                """,
                System.Text.Encoding.UTF8,
                "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePrescription_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PutAsync(
            "/api/prescriptions/1",
            new StringContent(
                """
                {
                    "id": 1,
                    "consultationId": 1,
                    "patientId": 1,
                    "doctorId": 1,
                    "medicationName": "Updated Medication",
                    "dosage": "20 mg",
                    "frequency": "Once daily",
                    "duration": "7 days"
                }
                """,
                System.Text.Encoding.UTF8,
                "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPrescriptionWithInvalidId_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/prescriptions/999999");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}