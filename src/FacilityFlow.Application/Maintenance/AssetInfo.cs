namespace FacilityFlow.Application.Maintenance;

public sealed record AssetInfo(
    string AssetNumber,
    string Description,
    string SiteId,
    string Location);
    

public sealed record ExternalRequest(
    string RequestNumber,
    string AssetNumber,
    string Status,
    string Description);


public sealed record CreateServiceRequest(
    string SiteId,
    string AssetNumber,
    string Description,
    string ReportedBy);
    
    
public sealed record CreateServiceRequestResult(
    bool Success,
    string? RequestNumber,
    string? ErrorMessage);