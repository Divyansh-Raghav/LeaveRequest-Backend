using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Smart_Service_Request_Manager.Models;
using Smart_Service_Request_Manager.Controllers;
using Smart_Service_Request_Manager.Services;
using Smart_Service_Request_Manager.Exceptions;

namespace Smart_Service_Request_Manager.Tests.Controllers;

public class ServiceRequestsControllerTests
{
    private readonly Mock<IServiceRequestService> _mockServiceRequestService;
    private readonly ServiceRequestsController _controller;

    public ServiceRequestsControllerTests()
    {
        _mockServiceRequestService = new Mock<IServiceRequestService>();
        _controller = new ServiceRequestsController(_mockServiceRequestService.Object);
    }

    [Fact]
    public async Task GetServiceRequests_ReturnsOkWithRequests()
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
            }
        };

        _mockServiceRequestService.Setup(x => x.GetServiceRequestsAsync(null, null)).ReturnsAsync(requests);

        // Act
        var result = await _controller.GetServiceRequests(null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        _mockServiceRequestService.Verify(x => x.GetServiceRequestsAsync(null, null), Times.Once);
    }

    [Fact]
    public async Task GetServiceRequests_WithUserFilter_ReturnsFilteredRequests()
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
            }
        };

        _mockServiceRequestService.Setup(x => x.GetServiceRequestsAsync(1, null)).ReturnsAsync(requests);

        // Act
        var result = await _controller.GetServiceRequests(1, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        _mockServiceRequestService.Verify(x => x.GetServiceRequestsAsync(1, null), Times.Once);
    }

    [Fact]
    public async Task GetServiceRequests_WithStatusFilter_ReturnsFilteredRequests()
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
            }
        };

        _mockServiceRequestService.Setup(x => x.GetServiceRequestsAsync(null, ServiceRequestStatus.Open)).ReturnsAsync(requests);

        // Act
        var result = await _controller.GetServiceRequests(null, "Open");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        _mockServiceRequestService.Verify(x => x.GetServiceRequestsAsync(null, ServiceRequestStatus.Open), Times.Once);
    }

    [Fact]
    public async Task GetServiceRequestById_WithValidId_ReturnsOkWithRequest()
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

        _mockServiceRequestService.Setup(x => x.GetServiceRequestByIdAsync(1)).ReturnsAsync(request);

        // Act
        var result = await _controller.GetServiceRequestById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        _mockServiceRequestService.Verify(x => x.GetServiceRequestByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetServiceRequestById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _mockServiceRequestService.Setup(x => x.GetServiceRequestByIdAsync(999))
            .ThrowsAsync(new ResourceNotFoundException("Service request not found"));

        // Act
        var result = await _controller.GetServiceRequestById(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task CreateServiceRequest_WithValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var requestDto = new CreateServiceRequestDto 
        { 
            Title = "Test Issue", 
            Description = "Test Description", 
            Priority = "High",
            CreatedByUserId = 1
        };

        var user = new User { Id = 1, Name = "John", Email = "john@example.com", Role = UserRole.Employee };
        var createdRequest = new ServiceRequest 
        { 
            Id = 1, 
            Title = requestDto.Title, 
            Description = requestDto.Description, 
            Priority = ServiceRequestPriority.High,
            Status = ServiceRequestStatus.Open,
            CreatedByUserId = 1,
            CreatedByUser = user,
            CreatedAt = DateTime.UtcNow
        };

        _mockServiceRequestService.Setup(x => x.CreateServiceRequestAsync(
            requestDto.Title, 
            requestDto.Description, 
            ServiceRequestPriority.High, 
            requestDto.CreatedByUserId
        )).ReturnsAsync(createdRequest);

        // Act
        var result = await _controller.CreateServiceRequest(requestDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(ServiceRequestsController.GetServiceRequestById), createdResult.ActionName);
        Assert.Equal(201, createdResult.StatusCode);
    }

    [Fact]
    public async Task CreateServiceRequest_WithInvalidUser_ReturnsBadRequest()
    {
        // Arrange
        var requestDto = new CreateServiceRequestDto 
        { 
            Title = "Test Issue", 
            Description = "Test Description", 
            Priority = "High",
            CreatedByUserId = 999
        };

        _mockServiceRequestService.Setup(x => x.CreateServiceRequestAsync(
            requestDto.Title, 
            requestDto.Description, 
            ServiceRequestPriority.High, 
            999
        )).ThrowsAsync(new ResourceNotFoundException("User not found"));

        // Act
        var result = await _controller.CreateServiceRequest(requestDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task UpdateServiceRequest_WithValidStatus_ReturnsOk()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John", Email = "john@example.com", Role = UserRole.Employee };
        var request = new ServiceRequest 
        { 
            Id = 1, 
            Title = "Issue 1", 
            Description = "Description here", 
            Priority = ServiceRequestPriority.High,
            Status = ServiceRequestStatus.InProgress,
            CreatedByUserId = 1,
            CreatedByUser = user,
            CreatedAt = DateTime.UtcNow
        };

        var updateDto = new UpdateServiceRequestDto { Status = "InProgress" };

        _mockServiceRequestService.Setup(x => x.UpdateServiceRequestStatusAsync(1, ServiceRequestStatus.InProgress))
            .ReturnsAsync(request);

        // Act
        var result = await _controller.UpdateServiceRequest(1, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _mockServiceRequestService.Verify(x => x.UpdateServiceRequestStatusAsync(1, ServiceRequestStatus.InProgress), Times.Once);
    }

    [Fact]
    public async Task UpdateServiceRequest_WithAssignment_ReturnsOk()
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
            AssignedToUserId = 2,
            AssignedToUser = assignee,
            CreatedAt = DateTime.UtcNow
        };

        var updateDto = new UpdateServiceRequestDto { AssignedToUserId = 2 };

        _mockServiceRequestService.Setup(x => x.AssignServiceRequestAsync(1, 2))
            .ReturnsAsync(request);

        // Act
        var result = await _controller.UpdateServiceRequest(1, updateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _mockServiceRequestService.Verify(x => x.AssignServiceRequestAsync(1, 2), Times.Once);
    }

    [Fact]
    public async Task UpdateServiceRequest_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new UpdateServiceRequestDto { Status = "InProgress" };

        _mockServiceRequestService.Setup(x => x.UpdateServiceRequestStatusAsync(999, ServiceRequestStatus.InProgress))
            .ThrowsAsync(new ResourceNotFoundException("Service request not found"));

        // Act
        var result = await _controller.UpdateServiceRequest(999, updateDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task UpdateServiceRequest_WithNoFields_ReturnsBadRequest()
    {
        // Arrange
        var updateDto = new UpdateServiceRequestDto { Status = null, AssignedToUserId = null };

        // The validation will fail because neither field is provided
        _controller.ModelState.AddModelError("", "At least one of Status or AssignedToUserId must be provided");

        // Act
        var result = await _controller.UpdateServiceRequest(1, updateDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }
}
