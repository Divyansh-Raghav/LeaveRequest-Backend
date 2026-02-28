using Microsoft.AspNetCore.Mvc;
using Smart_Service_Request_Manager.Models;
using Smart_Service_Request_Manager.Services;
using Smart_Service_Request_Manager.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace Smart_Service_Request_Manager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceRequestsController : ControllerBase
{
    private readonly IServiceRequestService _serviceRequestService;

    public ServiceRequestsController(IServiceRequestService serviceRequestService)
    {
        _serviceRequestService = serviceRequestService;
    }

    /// <summary>
    /// Get all service requests with optional filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ServiceRequestDto>>> GetServiceRequests(
        [FromQuery] int? userId = null,
        [FromQuery] string? status = null)
    {
        try
        {
            ServiceRequestStatus? statusEnum = null;
            
            if (!string.IsNullOrEmpty(status))
            {
                if (!Enum.TryParse<ServiceRequestStatus>(status, out var parsed))
                    return BadRequest(new { success = false, message = "Invalid status value", statusCode = 400 });

                statusEnum = parsed;
            }

            var requests = await _serviceRequestService.GetServiceRequestsAsync(userId, statusEnum);
            return Ok(new { success = true, data = requests.Select(sr => MapToDto(sr)).ToList() });
        }
        catch (ServiceValidationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, statusCode = 400 });
        }
    }

    /// <summary>
    /// Get service request by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceRequestDto>> GetServiceRequestById(int id)
    {
        try
        {
            var request = await _serviceRequestService.GetServiceRequestByIdAsync(id);
            return Ok(new { success = true, data = MapToDto(request) });
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
    /// Create a new service request
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ServiceRequestDto>> CreateServiceRequest([FromBody] CreateServiceRequestDto requestDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Validation failed", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

        try
        {
            var priority = Enum.Parse<ServiceRequestPriority>(requestDto.Priority);
            var serviceRequest = await _serviceRequestService.CreateServiceRequestAsync(
                requestDto.Title,
                requestDto.Description,
                priority,
                requestDto.CreatedByUserId
            );

            return CreatedAtAction(nameof(GetServiceRequestById), new { id = serviceRequest.Id }, new { success = true, data = MapToDto(serviceRequest) });
        }
        catch (ResourceNotFoundException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, statusCode = 400 });
        }
        catch (ServiceValidationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, statusCode = 400 });
        }
    }

    /// <summary>
    /// Update service request (status and/or assigned agent)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateServiceRequest(int id, [FromBody] UpdateServiceRequestDto updateDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Validation failed", errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });

        try
        {
            ServiceRequest? serviceRequest = null;

            // Update status if provided
            if (!string.IsNullOrEmpty(updateDto.Status))
            {
                if (!Enum.TryParse<ServiceRequestStatus>(updateDto.Status, out var statusEnum))
                    return BadRequest(new { success = false, message = "Invalid status value", statusCode = 400 });

                serviceRequest = await _serviceRequestService.UpdateServiceRequestStatusAsync(id, statusEnum);
            }

            // Update assigned user if provided
            if (updateDto.AssignedToUserId.HasValue)
            {
                serviceRequest = await _serviceRequestService.AssignServiceRequestAsync(id, updateDto.AssignedToUserId.Value);
            }

            return Ok(new { success = true, message = "Service request updated successfully", data = MapToDto(serviceRequest!) });
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

    // Helper method to map ServiceRequest to DTO
    private ServiceRequestDto MapToDto(ServiceRequest sr)
    {
        return new ServiceRequestDto
        {
            Id = sr.Id,
            Title = sr.Title,
            Description = sr.Description,
            Priority = sr.Priority.ToString(),
            Status = sr.Status.ToString(),
            CreatedByUserId = sr.CreatedByUserId,
            CreatedByUserName = sr.CreatedByUser?.Name ?? "Unknown",
            AssignedToUserId = sr.AssignedToUserId,
            AssignedToUserName = sr.AssignedToUser?.Name ?? "Unassigned",
            CreatedAt = sr.CreatedAt
        };
    }
}

// DTOs
public class ServiceRequestDto
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Priority { get; set; } = default!;
    public string Status { get; set; } = default!;
    public int CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = default!;
    public int? AssignedToUserId { get; set; }
    public string AssignedToUserName { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public class CreateServiceRequestDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
    public string Title { get; set; } = default!;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 2000 characters")]
    public string Description { get; set; } = default!;

    [Required(ErrorMessage = "Priority is required")]
    [RegularExpression("^(Low|Medium|High)$", ErrorMessage = "Priority must be 'Low', 'Medium', or 'High'")]
    public string Priority { get; set; } = default!;

    [Required(ErrorMessage = "CreatedByUserId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "CreatedByUserId must be greater than 0")]
    public int CreatedByUserId { get; set; }
}

public class UpdateServiceRequestDto
{
    [RegularExpression("^(Open|InProgress|Resolved|Closed)$", ErrorMessage = "Status must be 'Open', 'InProgress', 'Resolved', or 'Closed'")]
    public string? Status { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AssignedToUserId must be greater than 0")]
    public int? AssignedToUserId { get; set; }

    [CustomValidation(typeof(UpdateServiceRequestDto), nameof(ValidateAtLeastOneField))]
    public static ValidationResult? ValidateAtLeastOneField(UpdateServiceRequestDto dto, ValidationContext context)
    {
        if (string.IsNullOrEmpty(dto.Status) && !dto.AssignedToUserId.HasValue)
        {
            return new ValidationResult("At least one of Status or AssignedToUserId must be provided");
        }
        return ValidationResult.Success;
    }
}
