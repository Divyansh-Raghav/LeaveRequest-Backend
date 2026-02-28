using Microsoft.EntityFrameworkCore;
using Smart_Service_Request_Manager.Models;
using Smart_Service_Request_Manager.Exceptions;

namespace Smart_Service_Request_Manager.Services;

public interface IServiceRequestService
{
    Task<List<ServiceRequest>> GetAllServiceRequestsAsync();
    Task<ServiceRequest?> GetServiceRequestByIdAsync(int id);
    Task<List<ServiceRequest>> GetServiceRequestsByUserAsync(int userId);
    Task<List<ServiceRequest>> GetServiceRequestsByStatusAsync(ServiceRequestStatus status);
    Task<List<ServiceRequest>> GetServiceRequestsAsync(int? userId = null, ServiceRequestStatus? status = null);
    Task<ServiceRequest> CreateServiceRequestAsync(string title, string description, ServiceRequestPriority priority, int createdByUserId);
    Task<ServiceRequest> UpdateServiceRequestStatusAsync(int id, ServiceRequestStatus status);
    Task<ServiceRequest> AssignServiceRequestAsync(int id, int assignedToUserId);
}

public class ServiceRequestService : IServiceRequestService
{
    private readonly AppDbContext _context;

    public ServiceRequestService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all service requests
    /// </summary>
    public async Task<List<ServiceRequest>> GetAllServiceRequestsAsync()
    {
        return await _context.ServiceRequests
            .Include(sr => sr.CreatedByUser)
            .Include(sr => sr.AssignedToUser)
            .ToListAsync();
    }

    /// <summary>
    /// Get service request by ID
    /// </summary>
    public async Task<ServiceRequest?> GetServiceRequestByIdAsync(int id)
    {
        if (id <= 0)
            throw new ServiceValidationException("Service Request ID must be greater than 0");

        var request = await _context.ServiceRequests
            .Include(sr => sr.CreatedByUser)
            .Include(sr => sr.AssignedToUser)
            .FirstOrDefaultAsync(sr => sr.Id == id);

        if (request == null)
            throw new ResourceNotFoundException($"Service request with ID {id} not found");

        return request;
    }

    /// <summary>
    /// Get service requests created by or assigned to a user
    /// </summary>
    public async Task<List<ServiceRequest>> GetServiceRequestsByUserAsync(int userId)
    {
        if (userId <= 0)
            throw new ServiceValidationException("User ID must be greater than 0");

        return await _context.ServiceRequests
            .Include(sr => sr.CreatedByUser)
            .Include(sr => sr.AssignedToUser)
            .Where(sr => sr.CreatedByUserId == userId || sr.AssignedToUserId == userId)
            .ToListAsync();
    }

    /// <summary>
    /// Get service requests by status
    /// </summary>
    public async Task<List<ServiceRequest>> GetServiceRequestsByStatusAsync(ServiceRequestStatus status)
    {
        return await _context.ServiceRequests
            .Include(sr => sr.CreatedByUser)
            .Include(sr => sr.AssignedToUser)
            .Where(sr => sr.Status == status)
            .ToListAsync();
    }

    /// <summary>
    /// Get service requests with optional filters by user and/or status
    /// </summary>
    public async Task<List<ServiceRequest>> GetServiceRequestsAsync(int? userId = null, ServiceRequestStatus? status = null)
    {
        var query = _context.ServiceRequests
            .Include(sr => sr.CreatedByUser)
            .Include(sr => sr.AssignedToUser)
            .AsQueryable();

        if (userId.HasValue)
        {
            if (userId <= 0)
                throw new ServiceValidationException("User ID must be greater than 0");

            query = query.Where(sr => sr.CreatedByUserId == userId || sr.AssignedToUserId == userId);
        }

        if (status.HasValue)
        {
            query = query.Where(sr => sr.Status == status);
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// Create a new service request
    /// </summary>
    public async Task<ServiceRequest> CreateServiceRequestAsync(string title, string description, ServiceRequestPriority priority, int createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ServiceValidationException("Title cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            throw new ServiceValidationException("Description cannot be empty");

        if (createdByUserId <= 0)
            throw new ServiceValidationException("CreatedByUserId must be greater than 0");

        // Verify user exists
        var user = await _context.Users.FindAsync(createdByUserId);
        if (user == null)
            throw new ResourceNotFoundException($"User with ID {createdByUserId} not found");

        var serviceRequest = new ServiceRequest
        {
            Title = title,
            Description = description,
            Priority = priority,
            Status = ServiceRequestStatus.Open,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ServiceRequests.Add(serviceRequest);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(serviceRequest).Reference(sr => sr.CreatedByUser).LoadAsync();

        return serviceRequest;
    }

    /// <summary>
    /// Update service request status
    /// </summary>
    public async Task<ServiceRequest> UpdateServiceRequestStatusAsync(int id, ServiceRequestStatus status)
    {
        if (id <= 0)
            throw new ServiceValidationException("Service Request ID must be greater than 0");

        var request = await _context.ServiceRequests.FindAsync(id);
        if (request == null)
            throw new ResourceNotFoundException($"Service request with ID {id} not found");

        request.Status = status;
        _context.ServiceRequests.Update(request);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(request).Reference(sr => sr.CreatedByUser).LoadAsync();
        await _context.Entry(request).Reference(sr => sr.AssignedToUser).LoadAsync();

        return request;
    }

    /// <summary>
    /// Assign service request to a support agent
    /// </summary>
    public async Task<ServiceRequest> AssignServiceRequestAsync(int id, int assignedToUserId)
    {
        if (id <= 0)
            throw new ServiceValidationException("Service Request ID must be greater than 0");

        if (assignedToUserId <= 0)
            throw new ServiceValidationException("AssignedToUserId must be greater than 0");

        var request = await _context.ServiceRequests.FindAsync(id);
        if (request == null)
            throw new ResourceNotFoundException($"Service request with ID {id} not found");

        var assignedUser = await _context.Users.FindAsync(assignedToUserId);
        if (assignedUser == null)
            throw new ResourceNotFoundException($"User with ID {assignedToUserId} not found");

        request.AssignedToUserId = assignedToUserId;
        _context.ServiceRequests.Update(request);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        await _context.Entry(request).Reference(sr => sr.CreatedByUser).LoadAsync();
        await _context.Entry(request).Reference(sr => sr.AssignedToUser).LoadAsync();

        return request;
    }
}
