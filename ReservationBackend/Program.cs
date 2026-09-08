using System.Text.Json;
using FluentMigrator.Runner;
using ReservationBackend.Data;
using ReservationBackend.Endpoints;
using ReservationBackend.Exceptions;
using ReservationBackend.Migrations;
using ReservationBackend.Services;
using Scalar.AspNetCore;
using Serilog;

const string CliConnectionArgName = "CLIConnectionString";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddCommandLine(args,
    new Dictionary<string, string>
    {
        { "-c", CliConnectionArgName },
        { "--connection", CliConnectionArgName }
    }
);

var connectionString = builder.Configuration.GetConnectionString(CliConnectionArgName)
                       ?? builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("MySql database connection string not found");

builder.RegisterFluentMigrator(connectionString);

builder.Services
    .AddSingleton<IDbConnectionFactory>(_ => new MySqlConnectionFactory(connectionString))
    .AddScoped<AdminService>()
    .AddScoped<BookingService>()
    .AddExceptionHandler<ApiExceptionHandler>()
    .AddProblemDetails()
    .AddOpenApi()
    .AddSerilog();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(endpointPrefix: "/docs");
}

app.MapAdminEndpoints();
app.MapClientEndpoints();

app.MapGet("/", () => Results.Ok(new { message = "Reservation API is running." }));

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    Log.Information("Applying database migrations...");
    runner.MigrateUp();
    Log.Information("Database is up to date.");
}

app.Run();