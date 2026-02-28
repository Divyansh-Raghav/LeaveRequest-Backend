using Microsoft.AspNetCore.Mvc;
using Smart_Service_Request_Manager.Models;
using Smart_Service_Request_Manager.Services;
using Smart_Service_Request_Manager.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace Smart_Service_Request_Manager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(new { success = true, data = users.Select(u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role.ToString()
        }).ToList() });
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUserById(int id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            return Ok(new { success = true, data = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString()
            }});
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message, statusCode = 404 });
        }
        catch (ServiceValidationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, statusCode = 400 });
        }
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Validation failed", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

        try
        {
            var userRole = Enum.Parse<UserRole>(request.Role);
            var user = await _userService.CreateUserAsync(request.Name, request.Email, userRole);

            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new { success = true, data = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.ToString()
            }});
        }
        catch (ServiceValidationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, statusCode = 400 });
        }
    }
}

// DTOs
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Role { get; set; } = default!;
}

public class CreateUserDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = default!;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email is not valid")]
    public string Email { get; set; } = default!;

    [Required(ErrorMessage = "Role is required")]
    [RegularExpression("^(Employee|Support|Manager)$", ErrorMessage = "Role must be 'Employee', 'Support', or 'Manager'")]
    public string Role { get; set; } = default!;
}
