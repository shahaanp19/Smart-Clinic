namespace SmartClinicManagementSystem.Services.Interfaces;

public interface IAuthService
{
    Task<bool> ValidateCredentialsAsync(
        string email,
        string password);

    Task<bool> IsUserActiveAsync(string email);

    Task<string?> GetUserRoleAsync(string email);
}