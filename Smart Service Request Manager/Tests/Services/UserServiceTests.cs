using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Smart_Service_Request_Manager.Models;
using Smart_Service_Request_Manager.Services;
using Smart_Service_Request_Manager.Exceptions;
using System.Linq.Expressions;

namespace Smart_Service_Request_Manager.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<AppDbContext> _mockContext;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockContext = new Mock<AppDbContext>();
        _userService = new UserService(_mockContext.Object);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsListOfUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = 1, Name = "John Doe", Email = "john@example.com", Role = UserRole.Employee },
            new User { Id = 2, Name = "Jane Smith", Email = "jane@example.com", Role = UserRole.Support }
        };

        var mockDbSet = MockDbSet(users);
        _mockContext.Setup(x => x.Users).Returns(mockDbSet.Object);

        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("John Doe", result[0].Name);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John Doe", Email = "john@example.com", Role = UserRole.Employee };
        var users = new List<User> { user };

        var mockDbSet = MockDbSet(users);
        _mockContext.Setup(x => x.Users).Returns(mockDbSet.Object);

        // Act
        var result = await _userService.GetUserByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("John Doe", result.Name);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithInvalidId_ThrowsResourceNotFoundException()
    {
        // Arrange
        var users = new List<User>();
        var mockDbSet = MockDbSet(users);
        _mockContext.Setup(x => x.Users).Returns(mockDbSet.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(async () => 
            await _userService.GetUserByIdAsync(999));
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNegativeId_ThrowsServiceValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ServiceValidationException>(async () => 
            await _userService.GetUserByIdAsync(-1));
    }

    [Fact]
    public async Task GetUsersByRoleAsync_ReturnsUsersWithSpecificRole()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = 1, Name = "John Doe", Email = "john@example.com", Role = UserRole.Employee },
            new User { Id = 2, Name = "Jane Smith", Email = "jane@example.com", Role = UserRole.Support },
            new User { Id = 3, Name = "Bob Manager", Email = "bob@example.com", Role = UserRole.Employee }
        };

        var mockDbSet = MockDbSet(users);
        _mockContext.Setup(x => x.Users).Returns(mockDbSet.Object);

        // Act
        var result = await _userService.GetUsersByRoleAsync(UserRole.Employee);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, user => Assert.Equal(UserRole.Employee, user.Role));
    }

    [Fact]
    public async Task CreateUserAsync_WithValidData_CreatesAndReturnsUser()
    {
        // Arrange
        var mockDbSet = new Mock<DbSet<User>>();
        _mockContext.Setup(x => x.Users).Returns(mockDbSet.Object);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _userService.CreateUserAsync("John Doe", "john@example.com", UserRole.Employee);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal(UserRole.Employee, result.Role);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WithEmptyName_ThrowsServiceValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ServiceValidationException>(async () => 
            await _userService.CreateUserAsync("", "john@example.com", UserRole.Employee));
    }

    [Fact]
    public async Task CreateUserAsync_WithEmptyEmail_ThrowsServiceValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ServiceValidationException>(async () => 
            await _userService.CreateUserAsync("John Doe", "", UserRole.Employee));
    }

    // Helper method to create mock DbSet
    private Mock<DbSet<T>> MockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockDbSet = new Mock<DbSet<T>>();

        mockDbSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        mockDbSet.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));

        mockDbSet.As<IQueryable<T>>()
            .Setup(m => m.Expression)
            .Returns(queryable.Expression);

        mockDbSet.As<IQueryable<T>>()
            .Setup(m => m.ElementType)
            .Returns(queryable.ElementType);

        mockDbSet.As<IQueryable<T>>()
            .Setup(m => m.GetEnumerator())
            .Returns(queryable.GetEnumerator());

        return mockDbSet;
    }
}
