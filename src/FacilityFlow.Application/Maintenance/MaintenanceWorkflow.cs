namespace FacilityFlow.Application.Maintenance;

public sealed record ReportFault(
    string Phone,
    string SiteId,
    string AssetNumber,
    string Description);

public sealed record WorkflowResult(
    Guid? RequestId,
    string Status,
    string Message,
    string? SrNumber = null);

internal sealed record MaintenanceDraft(
    Guid Id,
    ReportFault Report,
    string Status,
    string? SrNumber = null);

public sealed class MaintenanceWorkflow
{
    private readonly IMaintenanceSystem _maintenance;
    private readonly ITechnicianDirectory _technicians;

    // Local prototype storage only
    private readonly Dictionary<Guid, MaintenanceDraft> _drafts = new();
    private readonly object _sync = new();

    public MaintenanceWorkflow(
        IMaintenanceSystem maintenance,
        ITechnicianDirectory technicians)
    {
        _maintenance = maintenance;
        _technicians = technicians;
    }

    public async Task<WorkflowResult> ReportAsync(
        ReportFault report,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(report.Phone) ||
            string.IsNullOrWhiteSpace(report.SiteId) ||
            string.IsNullOrWhiteSpace(report.AssetNumber) ||
            string.IsNullOrWhiteSpace(report.Description))
        {
            return new(null, "Rejected", "Missing required information");
        }

        if (!_technicians.IsAuthorized(report.Phone, report.SiteId))
        {
            return new(null, "Rejected", "Unauthorized technician");
        }

        var asset = await _maintenance.GetAssetAsync(
            report.SiteId,
            report.AssetNumber,
            ct);

        if (asset is null ||
            asset.SiteId != report.SiteId ||
            asset.AssetNumber != report.AssetNumber)
        {
            return new(null, "Rejected", "Invalid asset or site");
        }

        var openRequests = await _maintenance.FindOpenRequestsAsync(
            report.SiteId,
            report.AssetNumber,
            ct);

        if (openRequests.Count > 0)
        {
            return new(
                null,
                "Duplicate",
                "An open request already exists",
                openRequests[0].RequestNumber);
        }

        lock (_sync)
        {
            var existing = _drafts.Values.FirstOrDefault(x =>
                x.Report.SiteId == report.SiteId &&
                x.Report.AssetNumber == report.AssetNumber &&
                x.Status is "AwaitingConfirmation" or "Creating" or "Created");

            if (existing is not null)
            {
                return new(
                    existing.Id,
                    "Duplicate",
                    "An active local request already exists",
                    existing.SrNumber);
            }

            var draft = new MaintenanceDraft(
                Guid.NewGuid(),
                report,
                "AwaitingConfirmation");

            _drafts.Add(draft.Id, draft);

            return new(
                draft.Id,
                "AwaitingConfirmation",
                "Please confirm the maintenance request");
        }
    }

    public async Task<WorkflowResult> ConfirmAsync(
        Guid requestId,
        CancellationToken ct = default)
    {
        MaintenanceDraft draft;

        lock (_sync)
        {
            if (!_drafts.TryGetValue(requestId, out var found))
            {
                return new(null, "NotFound", "Request not found");
            }

            draft = found;

            if (draft.Status == "Created")
            {
                return new(
                    draft.Id,
                    "Created",
                    "Request already created",
                    draft.SrNumber);
            }

            if (draft.Status != "AwaitingConfirmation")
            {
                return new(
                    draft.Id,
                    draft.Status,
                    "Request cannot be confirmed again");
            }

            _drafts[requestId] = draft with
            {
                Status = "Creating"
            };
        }

        try
        {
            var result = await _maintenance.CreateServiceRequestAsync(
                new CreateServiceRequest(
                    draft.Report.SiteId,
                    draft.Report.AssetNumber,
                    draft.Report.Description,
                    draft.Report.Phone),
                ct);

            if (!result.Success ||
                string.IsNullOrWhiteSpace(result.RequestNumber))
            {
                throw new InvalidOperationException(
                    "Service request creation not confirmed");
            }

            lock (_sync)
            {
                _drafts[requestId] = draft with
                {
                    Status = "Created",
                    SrNumber = result.RequestNumber
                };
            }

            return new(
                requestId,
                "Created",
                "Service request created successfully",
                result.RequestNumber);
        }
        catch (Exception)
        {
            lock (_sync)
            {
                _drafts[requestId] = draft with
                {
                    Status = "NeedsReview"
                };
            }

            return new(
                requestId,
                "NeedsReview",
                "Creation outcome must be checked before retrying");
        }
    }
}