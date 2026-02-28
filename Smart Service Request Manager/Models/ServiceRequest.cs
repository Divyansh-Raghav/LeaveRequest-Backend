namespace Smart_Service_Request_Manager.Models;

public class ServiceRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ServiceRequestPriority Priority { get; set; }
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Open;
    public int CreatedByUserId { get; set; }
    public int? AssignedToUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User CreatedByUser { get; set; } = default!;
    public User? AssignedToUser { get; set; }
}
