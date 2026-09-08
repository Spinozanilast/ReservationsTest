using Dapper;
using MySqlConnector;
using ReservationBackend.Contracts;
using ReservationBackend.Data;
using ReservationBackend.Exceptions;

namespace ReservationBackend.Services;

public sealed class BookingService
{
    private readonly IDbConnectionFactory _database;

    public BookingService(IDbConnectionFactory database) => _database = database;

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

    public async Task<IReadOnlyList<SlotDto>> ListAvailableSlotsAsync(Guid serviceId, DateOnly date, CancellationToken ct)
    {
        await using var connection = await _database.CreateOpenConnectionAsync(ct);

        var serviceExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM Services WHERE Id = @ServiceId AND IsDeleted = 0);",
            new { ServiceId = serviceId },
            cancellationToken: ct));

        if (!serviceExists)
            throw new ReservationApiException(
                StatusCodes.Status404NotFound,
                "service_not_found",
                "The specified service does not exist.");

        var from = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = from.AddDays(1);

        const string sql = """
            SELECT s.Id, s.ServiceId, s.SlotStartTime, sv.Name AS ServiceName
            FROM Slots s
            INNER JOIN Services sv ON sv.Id = s.ServiceId
            WHERE s.ServiceId = @ServiceId
              AND s.IsDeleted = 0
              AND s.SlotStartTime >= GREATEST(@From, UTC_TIMESTAMP(3))
              AND s.SlotStartTime < @To
              AND NOT EXISTS (SELECT 1 FROM Reservations r WHERE r.SlotId = s.Id)
            ORDER BY s.SlotStartTime;
            """;

        var rows = await connection.QueryAsync<AvailableSlotRow>(new CommandDefinition(
            sql,
            new { ServiceId = serviceId, From = from, To = to },
            cancellationToken: ct));

        return rows.Select(r => new SlotDto(r.Id, r.ServiceId, r.SlotStartTime, r.ServiceName, IsBooked: false, Reservation: null)).ToList();
    }

    public async Task<ReservationDto> CreateReservationAsync(Guid slotId, string name, string phoneNumber, CancellationToken ct)
    {
        await using var connection = await _database.CreateOpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        const string slotSql = """
            SELECT s.Id, s.SlotStartTime, s.IsDeleted, sv.Name AS ServiceName
            FROM Slots s
            INNER JOIN Services sv ON sv.Id = s.ServiceId
            WHERE s.Id = @Id;
            """;

        var slot = await connection.QuerySingleOrDefaultAsync<SlotWithServiceRow>(new CommandDefinition(
            slotSql,
            new { Id = slotId },
            transaction,
            cancellationToken: ct));

        if (slot is null || slot.IsDeleted)
            throw new ReservationApiException(
                StatusCodes.Status404NotFound,
                "slot_not_found",
                "The specified slot does not exist.");

        if (slot.SlotStartTime <= DateTime.UtcNow)
            throw new ReservationApiException(
                StatusCodes.Status409Conflict,
                "slot_no_longer_available",
                "This slot is in the past and can no longer be booked.");

        const string insertSql = """
            INSERT INTO Reservations (Id, SlotId, Name, PhoneNumber, CreatedAt)
            VALUES (@Id, @SlotId, @Name, @PhoneNumber, UTC_TIMESTAMP(3));
            """;

        var reservationId = Guid.CreateVersion7();
        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                insertSql,
                new { Id = reservationId, SlotId = slotId, Name = name, PhoneNumber = phoneNumber },
                transaction,
                cancellationToken: ct));
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new ReservationApiException(
                StatusCodes.Status409Conflict,
                "slot_already_booked",
                "This time slot has just been booked by another client.");
        }

        await transaction.CommitAsync(ct);

        return new ReservationDto(reservationId, slotId, slot.SlotStartTime, slot.ServiceName, name, phoneNumber);
    }

    private sealed record ServiceRow(Guid Id, string Name, int DurationMinutes);

    private sealed record AvailableSlotRow(Guid Id, Guid ServiceId, DateTime SlotStartTime, string ServiceName);

    private sealed record SlotWithServiceRow(Guid Id, DateTime SlotStartTime, bool IsDeleted, string ServiceName);
}