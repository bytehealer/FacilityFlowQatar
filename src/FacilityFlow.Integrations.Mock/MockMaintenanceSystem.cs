using FacilityFlow.Application;
using FacilityFlow.Application.Maintenance;

namespace FacilityFlow.Integrations.Mock;

public sealed class MockMaintenanceSystem : IMaintenanceSystem
{
    private int _nextSr = 10000;
    private readonly object _sync = new();

    private readonly List<ExternalRequest> _requests =
    [
        new(
            "SR-9001",
            "AC-1002",
            "INPROG",
            "AC is not cooling")
    ];

    public Task<AssetInfo?> GetAssetAsync(
        string siteId,
        string assetNumber,
        CancellationToken cancellationToken = default)
    {
        if (siteId != "SITE01" ||
            (assetNumber != "AC-1001" &&
             assetNumber != "AC-1002"))
        {
            return Task.FromResult<AssetInfo?>(null);
        }

        AssetInfo asset = new(
            assetNumber,
            "Air Conditioner",
            siteId,
            assetNumber == "AC-1001" ? "ROOM-301" : "ROOM-302");

        return Task.FromResult<AssetInfo?>(asset);
    }

    public Task<IReadOnlyList<ExternalRequest>> FindOpenRequestsAsync(
        string siteId,
        string assetNumber,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            IReadOnlyList<ExternalRequest> result =
                siteId == "SITE01"
                    ? _requests.Where(x =>
                        x.AssetNumber == assetNumber).ToList()
                    : [];

            return Task.FromResult(result);
        }
    }

    public Task<CreateServiceRequestResult> CreateServiceRequestAsync(
        CreateServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            string srNumber = $"SR-{++_nextSr}";

            _requests.Add(new ExternalRequest(
                srNumber,
                request.AssetNumber,
                "NEW",
                request.Description));

            return Task.FromResult(
                new CreateServiceRequestResult(
                    true,
                    srNumber,
                    null));
        }
    }
}

public sealed class MockTechnicianDirectory : ITechnicianDirectory
{
    public bool IsAuthorized(string phone, string siteId)
    {
        return phone == "+97450000000" &&
               siteId == "SITE01";
    }
}