using SmartClinicManagementSystem.Models;

namespace SmartClinicManagementSystem.Services.Interfaces;

public interface IUserService
{
    Task<User?> GetByIdAsync(int id);

    Task<User?> GetByEmailAsync(string email);

    Task<IReadOnlyList<User>> GetAllAsync();

    Task<User> CreateAsync(User user);

    Task UpdateAsync(User user);

    Task<bool> DeactivateAsync(int id);
}