namespace MotoGarage.Application.DTOs.Admin;

public class AdminDashboardDto
{
    public int PendingBusinessApplications { get; set; }
    public int BlockedUsers { get; set; }
    public int TotalUsers { get; set; }

    public int TotalMechanics { get; set; }

    public int TotalStores { get; set; }

    public int TotalProducts { get; set; }

    public int TotalOrders { get; set; }

    public int PendingOrders { get; set; }

    public int PendingAppointments { get; set; }
}
