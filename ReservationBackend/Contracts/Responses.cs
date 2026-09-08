namespace ReservationBackend.Contracts;

public sealed record ServiceDto(Guid Id, string Name, int DurationMinutes);

public sealed record SlotDto(
    Guid Id,
    Guid ServiceId,
    DateTime StartTime,
    string ServiceName,
    bool IsBooked,
    ReservationSummaryDto? Reservation);

public sealed record ReservationSummaryDto(Guid Id, string Name, string PhoneNumber);

public sealed record ReservationDto(
    Guid Id,
    Guid SlotId,
    DateTime SlotStartTime,
    string ServiceName,
    string Name,
    string PhoneNumber);

public sealed record ApiError(string Code, string Message);