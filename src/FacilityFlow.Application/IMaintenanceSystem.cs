using FacilityFlow.Application.Maintenance;

namespace FacilityFlow.Application;

public interface IMaintenanceSystem
{
    Task<AssetInfo?> GetAssetAsync(
        string siteId,
        string assetNumber,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalRequest>> FindOpenRequestsAsync(
        string siteId,
        string assetNumber,
        CancellationToken cancellationToken = default);

    Task<CreateServiceRequestResult> CreateServiceRequestAsync(
        CreateServiceRequest request,
        CancellationToken cancellationToken = default);
}