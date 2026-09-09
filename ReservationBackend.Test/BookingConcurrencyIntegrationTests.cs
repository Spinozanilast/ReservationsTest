using Dapper;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using ReservationBackend.Data;
using ReservationBackend.Exceptions;
using ReservationBackend.Migrations;
using ReservationBackend.Services;
using Testcontainers.MySql;

namespace ReservationBackend.Test;

public sealed class BookingConcurrencyIntegrationTests : IAsyncLifetime
{
    private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.4")
        .WithDatabase("reservation_test")
        .WithUsername("reservation")
        .WithPassword("reservation")
        .Build();

    private string _connectionString = null!;
    private MySqlConnectionFactory _factory = null!;

    public async Task InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable("TEST_MYSQL_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(external))
        {
            _connectionString = external;
        }
        else
        {
            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
        }

        _connectionString = _connectionString.Contains("DateTimeKind=")
            ? _connectionString
            : _connectionString.TrimEnd(';') + ";DateTimeKind=Utc";

        _factory = new MySqlConnectionFactory(_connectionString);
        await ApplyMigrationsAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container.State == DotNet.Testcontainers.Containers.TestcontainersStates.Running)
            await _container.DisposeAsync();
    }

    private async Task ApplyMigrationsAsync()
    {
        using var services = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddMySql8()
                .WithGlobalConnectionString(_connectionString)
                .ScanIn(typeof(CreateServiceTable).Assembly).For.Migrations())
            .AddLogging()
            .BuildServiceProvider(false);

        using var scope = services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ParallelBookings_OnSameSlot_ExactlyOneSucceeds()
    {
        var serviceId = Guid.CreateVersion7();
        var slotId = Guid.CreateVersion7();
        var startTime = DateTime.UtcNow.AddDays(1);

        await using (var conn = await _factory.CreateOpenConnectionAsync())
        {
            await conn.ExecuteAsync(
                "INSERT INTO Services (Id, Name, DurationMinutes, CreatedAt, IsDeleted) VALUES (@Id, @Name, @DurationMinutes, UTC_TIMESTAMP(3), 0);",
                new { Id = serviceId, Name = "Consultation", DurationMinutes = 30 });

            await conn.ExecuteAsync(
                "INSERT INTO Slots (Id, ServiceId, SlotStartTime, CreatedAt, IsDeleted) VALUES (@Id, @ServiceId, @SlotStartTime, UTC_TIMESTAMP(3), 0);",
                new { Id = slotId, ServiceId = serviceId, SlotStartTime = startTime });
        }

        var booking = new BookingService(_factory);

        var t1 = Task.Run(() => booking.CreateReservationAsync(slotId, "Alice", "+375291111111", CancellationToken.None));
        var t2 = Task.Run(() => booking.CreateReservationAsync(slotId, "Bob", "+375292222222", CancellationToken.None));

        var succeeded = 0;
        var conflicts = 0;

        await Task.WhenAll(new[] { t1, t2 }.Select(t => t.ContinueWith(tt =>
        {
            if (tt.IsCompletedSuccessfully)
            {
                succeeded++;
            }
            else if (tt.IsFaulted)
            {
                var ex = tt.Exception!.GetBaseException();
                if (ex is ReservationApiException { Code: "slot_already_booked" })
                    conflicts++;
                else
                    throw new Exception("Unexpected failure in parallel booking.", ex);
            }
        })));

        Assert.Equal(1, succeeded);
        Assert.Equal(1, conflicts);
    }
}
