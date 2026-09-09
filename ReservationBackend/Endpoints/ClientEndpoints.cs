using ReservationBackend.Contracts;
using ReservationBackend.Services;
using ReservationBackend.Validation;

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
            var error = Validators.ValidateSlotServiceId(serviceId);
            if (error is not null)
                return TypedResults.BadRequest(new ApiError(error.Value.Code, error.Value.Message));

            if (date < DateOnly.FromDateTime(DateTime.UtcNow))
                return TypedResults.BadRequest(new ApiError("date_in_past", "The selected date cannot be in the past."));

            return TypedResults.Ok(await booking.ListAvailableSlotsAsync(serviceId, date, ct));
        });

        group.MapPost("/reservations", async Task<IResult> (CreateReservationRequest request, BookingService booking, CancellationToken ct) =>
        {
            var error = Validators.ValidateSlotId(request.SlotId);
            if (error is not null)
                return TypedResults.BadRequest(new ApiError(error.Value.Code, error.Value.Message));

            error = Validators.ValidateClientName(request.Name);
            if (error is not null)
                return TypedResults.BadRequest(new ApiError(error.Value.Code, error.Value.Message));

            var phoneNumber = Validators.NormalizePhoneNumber(request.PhoneNumber);
            if (phoneNumber is null)
                return TypedResults.BadRequest(new ApiError(
                    Validators.PhoneNumberInvalidCode,
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
}