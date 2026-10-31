using FacilityFlow.Application.Maintenance;
using FacilityFlow.Integrations.Mock;

namespace FacilityFlow.Tests;

public class MaintenanceWorkflowTests
{
    private static MaintenanceWorkflow CreateWorkflow() =>
        new(
            new MockMaintenanceSystem(),
            new MockTechnicianDirectory()
        );

    private static ReportFault ValidReport() =>
        new(
            "+97450000000",
            "SITE01",
            "AC-1001",
            "AC is not cooling"
        );

    [Fact]
    public async Task ValidReport_CreatesServiceRequest()
    {
        var workflow = CreateWorkflow();

        var report = await workflow.ReportAsync(ValidReport());

        Assert.Equal("AwaitingConfirmation", report.Status);
        Assert.NotNull(report.RequestId);

        var confirmed =
            await workflow.ConfirmAsync(report.RequestId.Value);

        Assert.Equal("Created", confirmed.Status);
        Assert.Equal("SR-10001", confirmed.SrNumber);
    }

    [Fact]
    public async Task DuplicateConfirmation_DoesNotCreateAnotherSR()
    {
        var workflow = CreateWorkflow();

        var report = await workflow.ReportAsync(ValidReport());
        Assert.NotNull(report.RequestId);

        var first =
            await workflow.ConfirmAsync(report.RequestId.Value);

        var second =
            await workflow.ConfirmAsync(report.RequestId.Value);

        Assert.Equal("Created", second.Status);
        Assert.Equal(first.SrNumber, second.SrNumber);
    }

    [Fact]
    public async Task UnauthorizedTechnician_IsRejected()
    {
        var workflow = CreateWorkflow();

        var result = await workflow.ReportAsync(
            ValidReport() with { Phone = "+97459999999" }
        );

        Assert.Equal("Rejected", result.Status);
    }

    [Fact]
    public async Task InvalidAsset_IsRejected()
    {
        var workflow = CreateWorkflow();

        var result = await workflow.ReportAsync(
            ValidReport() with { AssetNumber = "AC-9999" }
        );

        Assert.Equal("Rejected", result.Status);
    }

    [Fact]
    public async Task ExistingOpenRequest_IsDuplicate()
    {
        var workflow = CreateWorkflow();

        var result = await workflow.ReportAsync(
            ValidReport() with { AssetNumber = "AC-1002" }
        );

        Assert.Equal("Duplicate", result.Status);
        Assert.Equal("SR-9001", result.SrNumber);
    }

    [Fact]
    public async Task PendingRequest_IsNotDuplicated()
    {
        var workflow = CreateWorkflow();

        var first = await workflow.ReportAsync(ValidReport());
        var second = await workflow.ReportAsync(ValidReport());

        Assert.Equal("AwaitingConfirmation", first.Status);
        Assert.Equal("Duplicate", second.Status);
        Assert.Equal(first.RequestId, second.RequestId);
    }
}