namespace SmartClinicManagementSystem.Services.Interfaces;

public interface IAuthService
{
    Task<bool> ValidateCredentialsAsync(
        string email,
        string password);

    Task<string?> GetUserRoleAsync(string email);

    Task<int?> GetUserIdAsync(string email);

    Task<bool> IsUserActiveAsync(string email);

    Task<(int Id, string Role, DateTime UpdatedAtUtc)?>
        GetAuthenticationStateAsync(string email);

    Task<bool> IsAuthenticationStateValidAsync(
        int userId,
        string role,
        string updatedAtUtc);
}
