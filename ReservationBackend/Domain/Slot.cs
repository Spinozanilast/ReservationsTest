namespace ReservationBackend.Domain;

public class Slot : BaseDomainEntity
{
    public Guid ServiceId { get; set; }
    public DateTime SlotStartTime { get; set; }
}