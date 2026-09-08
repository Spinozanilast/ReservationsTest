using MySqlConnector;

namespace ReservationBackend.Data;

public interface IDbConnectionFactory
{
    Task<MySqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}

public sealed class MySqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public async Task<MySqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}