using ReservationBackend.Contracts;
using ReservationBackend.Services;
using ReservationBackend.Validation;

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
                var error = Validators.ValidateServiceName(request.Name)
                            ?? Validators.ValidateServiceDuration(request.DurationMinutes);
                if (error is not null)
                    return TypedResults.BadRequest(new ApiError(error.Value.Code, error.Value.Message));

                var service = await admin.CreateServiceAsync(request, ct);
                return TypedResults.Created($"/admin/services/{service.Id}", service);
            });

        group.MapGet("/services", async (AdminService admin, CancellationToken ct) =>
            TypedResults.Ok(await admin.ListServicesAsync(ct)));

        group.MapPost("/slots",
            async Task<IResult> (CreateSlotRequest request, AdminService admin, CancellationToken ct) =>
            {
                var error = Validators.ValidateSlotServiceId(request.ServiceId);
                if (error is not null)
                    return TypedResults.BadRequest(new ApiError(error.Value.Code, error.Value.Message));

                var startTime = EnsureUtc(request.StartTime);
                error = Validators.ValidateSlotStartTime(startTime);
                if (error is not null)
                    return TypedResults.BadRequest(new ApiError(error.Value.Code, error.Value.Message));

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

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };
}