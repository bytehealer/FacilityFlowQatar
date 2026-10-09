using FacilityFlow.Application;
using FacilityFlow.Application.Maintenance;
using FacilityFlow.Integrations.Mock;

var builder = WebApplication.CreateBuilder(args);

// Register external system adapters
builder.Services.AddSingleton<
    IMaintenanceSystem,
    MockMaintenanceSystem>();

builder.Services.AddSingleton<
    ITechnicianDirectory,
    MockTechnicianDirectory>();

// Singleton only because this prototype stores requests in memory
builder.Services.AddSingleton<MaintenanceWorkflow>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var dev = app.MapGroup("/api/dev");

    dev.MapPost("/report",
        async (
            ReportFault request,
            MaintenanceWorkflow workflow,
            CancellationToken ct) =>
        {
            var result = await workflow.ReportAsync(request, ct);
            return Results.Ok(result);
        });

    dev.MapPost("/requests/{id:guid}/confirm",
        async (
            Guid id,
            MaintenanceWorkflow workflow,
            CancellationToken ct) =>
        {
            var result = await workflow.ConfirmAsync(id, ct);
            return Results.Ok(result);
        });
}

app.Run();