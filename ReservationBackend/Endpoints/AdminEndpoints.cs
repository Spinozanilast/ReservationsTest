using ReservationBackend.Contracts;
using ReservationBackend.Services;

namespace ReservationBackend.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin")
            .WithTags("Admin");

        group.MapPost("/services",
            async Task<IResult> (CreateServiceRequest request, AdminService admin, CancellationToken ct) =>
            {
                var validation = ValidateCreateService(request);
                if (validation is not null) return validation;

                var service = await admin.CreateServiceAsync(request, ct);
                return TypedResults.Created($"/admin/services/{service.Id}", service);
            });

        group.MapGet("/services", async (AdminService admin, CancellationToken ct) =>
            TypedResults.Ok(await admin.ListServicesAsync(ct)));

        group.MapPost("/slots",
            async Task<IResult> (CreateSlotRequest request, AdminService admin, CancellationToken ct) =>
            {
                if (request.ServiceId == Guid.Empty)
                    return TypedResults.BadRequest(new ApiError("service_id_required", "ServiceId must be specified."));

                var startTime = NormalizeToUtc(request.StartTime);
                if (startTime <= DateTime.UtcNow)
                    return TypedResults.BadRequest(new ApiError("slot_start_in_past",
                        "The slot start time must be in the future."));

                var slot = await admin.CreateSlotAsync(new CreateSlotRequest(request.ServiceId, startTime), ct);
                return TypedResults.Created($"/admin/slots/{slot.Id}", slot);
            });

        group.MapGet("/slots", async (AdminService admin, CancellationToken ct) =>
            TypedResults.Ok(await admin.ListSlotsAsync(ct)));

        group.MapDelete("/slots/{id:guid}", async Task<IResult> (Guid id, AdminService admin, CancellationToken ct) =>
        {
            await admin.DeleteSlotAsync(id, ct);
            return TypedResults.NoContent();
        });

        group.MapGet("/reservations", async (AdminService admin, CancellationToken ct) =>
            TypedResults.Ok(await admin.ListReservationsAsync(ct)));

        group.MapDelete("/reservations/{id:guid}",
            async Task<IResult> (Guid id, AdminService admin, CancellationToken ct) =>
            {
                await admin.CancelReservationAsync(id, ct);
                return TypedResults.NoContent();
            });

        return app;
    }

    private static IResult? ValidateCreateService(CreateServiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return TypedResults.BadRequest(new ApiError("service_name_required", "Service name must not be empty."));

        if (request.DurationMinutes <= 0)
            return TypedResults.BadRequest(new ApiError("service_duration_invalid",
                "Service duration must be greater than zero."));

        return null;
    }

    private static DateTime NormalizeToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };
}