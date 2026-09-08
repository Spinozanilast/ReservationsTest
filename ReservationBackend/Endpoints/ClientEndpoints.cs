using ReservationBackend.Contracts;
using ReservationBackend.Services;

namespace ReservationBackend.Endpoints;

public static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/")
            .WithTags("Client");

        group.MapGet("/services", async (BookingService booking, CancellationToken ct) =>
            TypedResults.Ok(await booking.ListServicesAsync(ct)));

        group.MapGet("/slots/available", async Task<IResult> (Guid serviceId, DateOnly date, BookingService booking, CancellationToken ct) =>
        {
            if (serviceId == Guid.Empty)
                return TypedResults.BadRequest(new ApiError("service_id_required", "ServiceId must be specified."));

            if (date < DateOnly.FromDateTime(DateTime.UtcNow))
                return TypedResults.BadRequest(new ApiError("date_in_past", "The selected date cannot be in the past."));

            return TypedResults.Ok(await booking.ListAvailableSlotsAsync(serviceId, date, ct));
        });

        group.MapPost("/reservations", async Task<IResult> (CreateReservationRequest request, BookingService booking, CancellationToken ct) =>
        {
            if (request.SlotId == Guid.Empty)
                return TypedResults.BadRequest(new ApiError("slot_id_required", "SlotId must be specified."));

            if (string.IsNullOrWhiteSpace(request.Name))
                return TypedResults.BadRequest(new ApiError("client_name_required", "Name must not be empty."));

            var phoneNumber = NormalizePhoneNumber(request.PhoneNumber);
            if (phoneNumber is null)
                return TypedResults.BadRequest(new ApiError(
                    "phone_number_invalid",
                    "Phone number is required and must contain 7-15 digits with an optional leading '+', e.g. +375291234567."));

            var reservation = await booking.CreateReservationAsync(
                request.SlotId,
                request.Name.Trim(),
                phoneNumber,
                ct);

            return TypedResults.Created($"/reservations/{reservation.Id}", reservation);
        });

        return app;
    }

    private static string? NormalizePhoneNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var trimmed = phone.Trim();
        foreach (var ch in trimmed)
        {
            if (char.IsAsciiDigit(ch) || ch is '+' or '-' or '(' or ')' or ' ')
                continue;
            return null;
        }

        var plusCount = trimmed.Count(ch => ch == '+');
        if (plusCount > 1 || (plusCount == 1 && !trimmed.StartsWith('+')))
            return null;

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length is < 7 or > 15)
            return null;

        return plusCount == 1 ? $"+{digits}" : digits;
    }
}