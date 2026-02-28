using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Smart_Service_Request_Manager.Models;
using Smart_Service_Request_Manager.Controllers;
using Smart_Service_Request_Manager.Services;
using Smart_Service_Request_Manager.Exceptions;

namespace Smart_Service_Request_Manager.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _controller = new UsersController(_mockUserService.Object);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsOkWithUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = 1, Name = "John Doe", Email = "john@example.com", Role = UserRole.Employee },
            new User { Id = 2, Name = "Jane Smith", Email = "jane@example.com", Role = UserRole.Support }
        };

        _mockUserService.Setup(x => x.GetAllUsersAsync()).ReturnsAsync(users);

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        _mockUserService.Verify(x => x.GetAllUsersAsync(), Times.Once);
    }

    [Fact]
    public async Task GetUserById_WithValidId_ReturnsOkWithUser()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John Doe", Email = "john@example.com", Role = UserRole.Employee };
        _mockUserService.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _controller.GetUserById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        _mockUserService.Verify(x => x.GetUserByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetUserById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockUserService.Setup(x => x.GetUserByIdAsync(999))
            .ThrowsAsync(new ResourceNotFoundException("User not found"));

        // Act
        var result = await _controller.GetUserById(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
        _mockUserService.Verify(x => x.GetUserByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetUserById_WithNegativeId_ReturnsBadRequest()
    {
        // Arrange
        _mockUserService.Setup(x => x.GetUserByIdAsync(-1))
            .ThrowsAsync(new ServiceValidationException("User ID must be greater than 0"));

        // Act
        var result = await _controller.GetUserById(-1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateUserDto 
        { 
            Name = "John Doe", 
            Email = "john@example.com", 
            Role = "Employee" 
        };
        var user = new User 
        { 
            Id = 1, 
            Name = request.Name, 
            Email = request.Email, 
            Role = UserRole.Employee 
        };

        _mockUserService.Setup(x => x.CreateUserAsync(request.Name, request.Email, UserRole.Employee))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(UsersController.GetUserById), createdResult.ActionName);
        Assert.Equal(201, createdResult.StatusCode);
        _mockUserService.Verify(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>()), Times.Once);
    }

    [Fact]
    public async Task CreateUser_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateUserDto 
        { 
            Name = "", 
            Email = "john@example.com", 
            Role = "Employee" 
        };

        _mockUserService.Setup(x => x.CreateUserAsync("", "john@example.com", UserRole.Employee))
            .ThrowsAsync(new ServiceValidationException("Name cannot be empty"));

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateUserDto 
        { 
            Name = "John Doe", 
            Email = "invalid-email", 
            Role = "Employee" 
        };

        // Act - The ModelState validation will catch this before service call
        _controller.ModelState.AddModelError("Email", "Email is not valid");
        var result = await _controller.CreateUser(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithInvalidRole_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateUserDto 
        { 
            Name = "John Doe", 
            Email = "john@example.com", 
            Role = "InvalidRole" 
        };

        // Act - The ModelState validation will catch this before service call
        _controller.ModelState.AddModelError("Role", "Role must be 'Employee', 'Support', or 'Manager'");
        var result = await _controller.CreateUser(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }
}
