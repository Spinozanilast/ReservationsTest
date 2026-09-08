namespace ReservationBackend.Contracts;

public sealed record CreateServiceRequest(string Name, int DurationMinutes);

public sealed record CreateSlotRequest(Guid ServiceId, DateTime StartTime);

public sealed record CreateReservationRequest(Guid SlotId, string Name, string PhoneNumber);