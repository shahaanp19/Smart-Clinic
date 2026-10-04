using System.Net;
using Xunit;

namespace SmartClinicManagementSystem.Tests.Integration;

public class MedicalRecordsApiIntegrationTests
    : IClassFixture<ApiIntegrationTestFactory>
{
    private readonly HttpClient _client;

    public MedicalRecordsApiIntegrationTests(ApiIntegrationTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMedicalRecord_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/medical-records/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPatientMedicalRecords_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/medical-records/patient/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDoctorMedicalRecords_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/medical-records/doctor/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateMedicalRecord_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync(
            "/api/medical-records",
            new StringContent(
                """
                {
                    "patientId": 1,
                    "doctorId": 1,
                    "recordType": "Consultation",
                    "description": "Test medical record"
                }
                """,
                System.Text.Encoding.UTF8,
                "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMedicalRecord_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PutAsync(
            "/api/medical-records/1",
            new StringContent(
                """
                {
                    "id": 1,
                    "patientId": 1,
                    "doctorId": 1,
                    "recordType": "Consultation",
                    "description": "Updated medical record"
                }
                """,
                System.Text.Encoding.UTF8,
                "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMedicalRecordWithInvalidId_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/medical-records/999999");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMedicalRecordsWithInvalidRoute_ReturnsNotFound()
    {
        var response = await _client.GetAsync(
            "/api/medical-records/not-a-number");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}