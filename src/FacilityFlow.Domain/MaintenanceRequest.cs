namespace FacilityFlow.Domain;

public class MaintenanceRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string TechnicianPhone { get; set; } = null!;

    public string SiteId { get; set; } = null!;

    public string AssetNumber { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string ExternalMessageId { get; set; } = null!;

    public MaintenanceRequestStatus Status { get; set; }
}

public enum MaintenanceRequestStatus
{
    Received,
    Validating,
    Duplicate,
    AwaitingConfirmation,
    Creating,
    Created,
    Rejected,
    Failed
}