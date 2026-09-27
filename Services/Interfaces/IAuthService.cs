namespace SmartClinicManagementSystem.Services.Interfaces;

public interface IAuthService
{
    Task<bool> ValidateCredentialsAsync(
        string email,
        string password);

    Task<string?> GetUserRoleAsync(string email);

    Task<int?> GetUserIdAsync(string email);

    Task<bool> IsUserActiveAsync(string email);
}