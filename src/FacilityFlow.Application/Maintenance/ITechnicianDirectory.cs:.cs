namespace FacilityFlow.Application.Maintenance;

public interface ITechnicianDirectory
{
    bool IsAuthorized(string phone, string siteId);
}