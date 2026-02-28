using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Smart_Service_Request_Manager.Models;
using Smart_Service_Request_Manager.Services;
using Smart_Service_Request_Manager.Exceptions;
using System.Linq.Expressions;

namespace Smart_Service_Request_Manager.Tests.Services;

public class ServiceRequestServiceTests
{
    private readonly Mock<AppDbContext> _mockContext;
    private readonly ServiceRequestService _serviceRequestService;

    public ServiceRequestServiceTests()
    {
        _mockContext = new Mock<AppDbContext>();
        _serviceRequestService = new ServiceRequestService(_mockContext.Object);
    }

    [Fact]
    public async Task GetAllServiceRequestsAsync_ReturnsAllRequests()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John", Email = "john@example.com", Role = UserRole.Employee };
        var requests = new List<ServiceRequest>
        {
            new ServiceRequest 
            { 
                Id = 1, 
                Title = "Issue 1", 
                Description = "Description here", 
                Priority = ServiceRequestPriority.High,
                Status = ServiceRequestStatus.Open,
                CreatedByUserId = 1,
                CreatedByUser = user,
                CreatedAt = DateTime.UtcNow
            },
            new ServiceRequest 
            { 
                Id = 2, 
                Title = "Issue 2", 
                Description = "Description here", 
                Priority = ServiceRequestPriority.Low,
                Status = ServiceRequestStatus.InProgress,
                CreatedByUserId = 1,
                CreatedByUser = user,
                CreatedAt = DateTime.UtcNow
            }
        };

        var mockDbSet = MockDbSet(requests);
        _mockContext.Setup(x => x.ServiceRequests).Returns(mockDbSet.Object);

        // Act
        var result = await _serviceRequestService.GetAllServiceRequestsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetServiceRequestByIdAsync_WithValidId_ReturnsRequest()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John", Email = "john@example.com", Role = UserRole.Employee };
        var request = new ServiceRequest 
        { 
            Id = 1, 
            Title = "Issue 1", 
            Description = "Description here", 
            Priority = ServiceRequestPriority.High,
            Status = ServiceRequestStatus.Open,
            CreatedByUserId = 1,
            CreatedByUser = user,
            CreatedAt = DateTime.UtcNow
        };
        var requests = new List<ServiceRequest> { request };

        var mockDbSet = MockDbSet(requests);
        _mockContext.Setup(x => x.ServiceRequests).Returns(mockDbSet.Object);

        // Act
        var result = await _serviceRequestService.GetServiceRequestByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Issue 1", result.Title);
    }

    [Fact]
    public async Task GetServiceRequestByIdAsync_WithInvalidId_ThrowsResourceNotFoundException()
    {
        // Arrange
        var requests = new List<ServiceRequest>();
        var mockDbSet = MockDbSet(requests);
        _mockContext.Setup(x => x.ServiceRequests).Returns(mockDbSet.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(async () => 
            await _serviceRequestService.GetServiceRequestByIdAsync(999));
    }

    [Fact]
    public async Task GetServiceRequestByIdAsync_WithNegativeId_ThrowsServiceValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ServiceValidationException>(async () => 
            await _serviceRequestService.GetServiceRequestByIdAsync(-1));
    }

    [Fact]
    public async Task GetServiceRequestsByUserAsync_ReturnsRequestsByUser()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John", Email = "john@example.com", Role = UserRole.Employee };
        var requests = new List<ServiceRequest>
        {
            new ServiceRequest 
            { 
                Id = 1, 
                Title = "Issue 1", 
                Description = "Description here", 
                Priority = ServiceRequestPriority.High,
                Status = ServiceRequestStatus.Open,
                CreatedByUserId = 1,
                CreatedByUser = user,
                CreatedAt = DateTime.UtcNow
            },
            new ServiceRequest 
            { 
                Id = 2, 
                Title = "Issue 2", 
                Description = "Description here", 
                Priority = ServiceRequestPriority.Low,
                Status = ServiceRequestStatus.InProgress,
                CreatedByUserId = 2,
                CreatedByUser = user,
                CreatedAt = DateTime.UtcNow
            }
        };

        var mockDbSet = MockDbSet(requests);
        _mockContext.Setup(x => x.ServiceRequests).Returns(mockDbSet.Object);

        // Act
        var result = await _serviceRequestService.GetServiceRequestsByUserAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(1, result[0].CreatedByUserId);
    }

    [Fact]
    public async Task GetServiceRequestsByStatusAsync_ReturnsRequestsByStatus()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John", Email = "john@example.com", Role = UserRole.Employee };
        var requests = new List<ServiceRequest>
        {
            new ServiceRequest 
            { 
                Id = 1, 
                Title = "Issue 1", 
                Description = "Description here", 
                Priority = ServiceRequestPriority.High,
                Status = ServiceRequestStatus.Open,
                CreatedByUserId = 1,
                CreatedByUser = user,
                CreatedAt = DateTime.UtcNow
            },
            new ServiceRequest 
            { 
                Id = 2, 
                Title = "Issue 2", 
                Description = "Description here", 
                Priority = ServiceRequestPriority.Low,
                Status = ServiceRequestStatus.InProgress,
                CreatedByUserId = 1,
                CreatedByUser = user,
                CreatedAt = DateTime.UtcNow
            }
        };

        var mockDbSet = MockDbSet(requests);
        _mockContext.Setup(x => x.ServiceRequests).Returns(mockDbSet.Object);

        // Act
        var result = await _serviceRequestService.GetServiceRequestsByStatusAsync(ServiceRequestStatus.Open);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(ServiceRequestStatus.Open, result[0].Status);
    }

    [Fact]
    public async Task CreateServiceRequestAsync_WithValidData_CreatesRequest()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John", Email = "john@example.com", Role = UserRole.Employee };
        var mockDbSet = new Mock<DbSet<User>>();
        var mockSRDbSet = new Mock<DbSet<ServiceRequest>>();
        
        _mockContext.Setup(x => x.Users).Returns(mockDbSet.Object);
        _mockContext.Setup(x => x.ServiceRequests).Returns(mockSRDbSet.Object);
        _mockContext.Setup(x => x.Users.FindAsync(It.IsAny<object[]>())).ReturnsAsync(user);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _serviceRequestService.CreateServiceRequestAsync(
            "Test Title", 
            "Test Description", 
            ServiceRequestPriority.High, 
            1
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Title", result.Title);
        Assert.Equal(ServiceRequestStatus.Open, result.Status);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateServiceRequestAsync_WithNonExistentUser_ThrowsResourceNotFoundException()
    {
        // Arrange
        _mockContext.Setup(x => x.Users.FindAsync(It.IsAny<object[]>())).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ResourceNotFoundException>(async () => 
            await _serviceRequestService.CreateServiceRequestAsync(
                "Test Title", 
                "Test Description", 
                ServiceRequestPriority.High, 
                999
            ));
    }

    [Fact]
    public async Task CreateServiceRequestAsync_WithEmptyTitle_ThrowsServiceValidationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ServiceValidationException>(async () => 
            await _serviceRequestService.CreateServiceRequestAsync(
                "", 
                "Test Description", 
                ServiceRequestPriority.High, 
                1
            ));
    }

    [Fact]
    public async Task UpdateServiceRequestStatusAsync_WithValidData_UpdatesStatus()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John", Email = "john@example.com", Role = UserRole.Employee };
        var request = new ServiceRequest 
        { 
            Id = 1, 
            Title = "Issue 1", 
            Description = "Description here", 
            Priority = ServiceRequestPriority.High,
            Status = ServiceRequestStatus.Open,
            CreatedByUserId = 1,
            CreatedByUser = user,
            CreatedAt = DateTime.UtcNow
        };

        _mockContext.Setup(x => x.ServiceRequests.FindAsync(It.IsAny<object[]>())).ReturnsAsync(request);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _serviceRequestService.UpdateServiceRequestStatusAsync(1, ServiceRequestStatus.InProgress);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ServiceRequestStatus.InProgress, result.Status);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignServiceRequestAsync_WithValidData_AssignsUser()
    {
        // Arrange
        var creator = new User { Id = 1, Name = "John", Email = "john@example.com", Role = UserRole.Employee };
        var assignee = new User { Id = 2, Name = "Jane", Email = "jane@example.com", Role = UserRole.Support };
        var request = new ServiceRequest 
        { 
            Id = 1, 
            Title = "Issue 1", 
            Description = "Description here", 
            Priority = ServiceRequestPriority.High,
            Status = ServiceRequestStatus.Open,
            CreatedByUserId = 1,
            CreatedByUser = creator,
            CreatedAt = DateTime.UtcNow
        };

        _mockContext.Setup(x => x.ServiceRequests.FindAsync(It.IsAny<object[]>())).ReturnsAsync(request);
        _mockContext.Setup(x => x.Users.FindAsync(It.IsAny<object[]>())).ReturnsAsync(assignee);
        _mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _serviceRequestService.AssignServiceRequestAsync(1, 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.AssignedToUserId);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
