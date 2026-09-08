namespace ReservationBackend.Domain;

public class Reservation: BaseDomainEntity
{
    public Guid SlotId { get; set; }    
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}