using Dapper;
using MySqlConnector;
using ReservationBackend.Contracts;
using ReservationBackend.Data;
using ReservationBackend.Exceptions;

namespace ReservationBackend.Services;

public sealed class AdminService
{
    private readonly IDbConnectionFactory _database;

    public AdminService(IDbConnectionFactory database) => _database = database;

    public async Task<ServiceDto> CreateServiceAsync(CreateServiceRequest request, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO Services (Id, Name, DurationMinutes, CreatedAt, IsDeleted)
            VALUES (@Id, @Name, @DurationMinutes, UTC_TIMESTAMP(3), 0);
            """;

        var serviceId = Guid.CreateVersion7();
        await using var connection = await _database.CreateOpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = serviceId, request.Name, request.DurationMinutes },
            cancellationToken: ct));

        return new ServiceDto(serviceId, request.Name, request.DurationMinutes);
    }

    public async Task<IReadOnlyList<ServiceDto>> ListServicesAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT Id, Name, DurationMinutes
            FROM Services
            WHERE IsDeleted = 0
            ORDER BY Name;
            """;

        await using var connection = await _database.CreateOpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<ServiceRow>(new CommandDefinition(sql, null, cancellationToken: ct));
        return rows.Select(r => new ServiceDto(r.Id, r.Name, r.DurationMinutes)).ToList();
    }

    public async Task<SlotDto> CreateSlotAsync(CreateSlotRequest request, CancellationToken ct)
    {
        await using var connection = await _database.CreateOpenConnectionAsync(ct);

        var serviceName = await connection.ExecuteScalarAsync<string>(new CommandDefinition(
            "SELECT Name FROM Services WHERE Id = @ServiceId AND IsDeleted = 0;",
            new { request.ServiceId },
            cancellationToken: ct));

        if (serviceName is null)
            throw new ReservationApiException(
                StatusCodes.Status404NotFound,
                "service_not_found",
                "The specified service does not exist.");

        const string sql = """
            INSERT INTO Slots (Id, ServiceId, SlotStartTime, CreatedAt, IsDeleted)
            VALUES (@Id, @ServiceId, @SlotStartTime, UTC_TIMESTAMP(3), 0);
            """;

        var slotId = Guid.CreateVersion7();
        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                sql,
                new { Id = slotId, request.ServiceId, request.StartTime },
                cancellationToken: ct));
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new ReservationApiException(
                StatusCodes.Status409Conflict,
                "slot_already_exists",
                "A slot for this service at the same time already exists.");
        }

        return new SlotDto(slotId, request.ServiceId, request.StartTime, serviceName, IsBooked: false, Reservation: null);
    }

    public async Task<IReadOnlyList<SlotDto>> ListSlotsAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT s.Id, s.ServiceId, s.SlotStartTime, sv.Name AS ServiceName,
                   r.Id AS ReservationId, r.Name AS ReservationName, r.PhoneNumber AS ReservationPhoneNumber
            FROM Slots s
            INNER JOIN Services sv ON sv.Id = s.ServiceId
            LEFT JOIN Reservations r ON r.SlotId = s.Id
            WHERE s.IsDeleted = 0
            ORDER BY s.SlotStartTime;
            """;

        await using var connection = await _database.CreateOpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<SlotListingRow>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.Select(r => new SlotDto(
                r.Id,
                r.ServiceId,
                r.SlotStartTime,
                r.ServiceName,
                IsBooked: r.ReservationId.HasValue,
                r.ReservationId is null
                    ? null
                    : new ReservationSummaryDto(r.ReservationId.Value, r.ReservationName!, r.ReservationPhoneNumber!)))
            .ToList();
    }

    public async Task DeleteSlotAsync(Guid slotId, CancellationToken ct)
    {
        await using var connection = await _database.CreateOpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM Slots WHERE Id = @Id AND IsDeleted = 0);",
            new { Id = slotId },
            transaction,
            cancellationToken: ct));

        if (!exists)
            throw new ReservationApiException(
                StatusCodes.Status404NotFound,
                "slot_not_found",
                "The specified slot does not exist.");

        var isBooked = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM Reservations r WHERE r.SlotId = @SlotId);",
            new { SlotId = slotId },
            transaction,
            cancellationToken: ct));

        if (isBooked)
            throw new ReservationApiException(
                StatusCodes.Status409Conflict,
                "slot_has_reservation",
                "Cannot delete a slot that already has a reservation.");

        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE Slots SET IsDeleted = 1, LastModifiedAt = UTC_TIMESTAMP(3) WHERE Id = @Id;",
            new { Id = slotId },
            transaction,
            cancellationToken: ct));

        await transaction.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<ReservationDto>> ListReservationsAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT r.Id, r.SlotId, r.Name, r.PhoneNumber, s.SlotStartTime, sv.Name AS ServiceName
            FROM Reservations r
            INNER JOIN Slots s ON s.Id = r.SlotId
            INNER JOIN Services sv ON sv.Id = s.ServiceId
            ORDER BY s.SlotStartTime;
            """;

        await using var connection = await _database.CreateOpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<ReservationRow>(new CommandDefinition(sql, null, cancellationToken: ct));
        return rows.Select(r =>
                new ReservationDto(r.Id, r.SlotId, r.SlotStartTime, r.ServiceName, r.Name, r.PhoneNumber))
            .ToList();
    }

    public async Task CancelReservationAsync(Guid reservationId, CancellationToken ct)
    {
        const string sql = "DELETE FROM Reservations WHERE Id = @Id;";

        await using var connection = await _database.CreateOpenConnectionAsync(ct);
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = reservationId },
            cancellationToken: ct));

        if (affected == 0)
            throw new ReservationApiException(
                StatusCodes.Status404NotFound,
                "reservation_not_found",
                "The specified reservation does not exist.");
    }

    private sealed record ServiceRow(Guid Id, string Name, int DurationMinutes);

    private sealed record SlotListingRow(
        Guid Id,
        Guid ServiceId,
        DateTime SlotStartTime,
        string ServiceName,
        Guid? ReservationId,
        string? ReservationName,
        string? ReservationPhoneNumber);

    private sealed record ReservationRow(
        Guid Id,
        Guid SlotId,
        string Name,
        string PhoneNumber,
        DateTime SlotStartTime,
        string ServiceName);
}