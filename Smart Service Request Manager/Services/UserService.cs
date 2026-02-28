using Microsoft.EntityFrameworkCore;
using Smart_Service_Request_Manager.Models;
using Smart_Service_Request_Manager.Exceptions;

namespace Smart_Service_Request_Manager.Services;

public interface IUserService
{
    Task<List<User>> GetAllUsersAsync();
    Task<User?> GetUserByIdAsync(int id);
    Task<List<User>> GetUsersByRoleAsync(UserRole role);
    Task<User> CreateUserAsync(string name, string email, UserRole role);
}

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    public async Task<User?> GetUserByIdAsync(int id)
    {
        if (id <= 0)
            throw new ServiceValidationException("User ID must be greater than 0");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            throw new ResourceNotFoundException($"User with ID {id} not found");

        return user;
    }

    /// <summary>
    /// Get users by role (Employee, Support, Manager)
    /// </summary>
    public async Task<List<User>> GetUsersByRoleAsync(UserRole role)
    {
        return await _context.Users.Where(u => u.Role == role).ToListAsync();
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    public async Task<User> CreateUserAsync(string name, string email, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ServiceValidationException("Name cannot be empty");

        if (string.IsNullOrWhiteSpace(email))
            throw new ServiceValidationException("Email cannot be empty");

        var user = new User
        {
            Name = name,
            Email = email,
            Role = role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }
}
