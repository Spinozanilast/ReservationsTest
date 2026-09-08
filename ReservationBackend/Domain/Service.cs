namespace ReservationBackend.Domain;

public class Service : BaseDomainEntity
{
    public string Name { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}