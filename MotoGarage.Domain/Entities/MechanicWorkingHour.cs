namespace MotoGarage.Domain.Entities;

public class MechanicWorkingHour
{
    public int Id { get; set; }
    public int MechanicId { get; set; }
    public int DayOfWeek { get; set; }
    public bool IsOpen { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public Mechanic Mechanic { get; set; } = null!;
}
