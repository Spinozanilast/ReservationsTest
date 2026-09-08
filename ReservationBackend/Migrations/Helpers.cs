using System.Reflection;
using FluentMigrator.Runner;
using Serilog;
using ITableDefinition = FluentMigrator.Builders.Create.Table.ICreateTableWithColumnOrSchemaOrDescriptionSyntax;

namespace ReservationBackend.Migrations;

public static class Helpers
{
    public static ITableDefinition AddBaseDomainEntityColumns(
        this ITableDefinition table)
    {
        table.WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("CreatedAt").AsCustom("datetime(3)").NotNullable()
            .WithColumn("CreatedBy").AsString(255).Nullable()
            .WithColumn("LastModifiedAt").AsCustom("datetime(3)").Nullable()
            .WithColumn("LastModifiedBy").AsString(255).Nullable()
            .WithColumn("IsDeleted").AsBoolean().NotNullable().WithDefaultValue(false);

        return table;
    }

    public static void RegisterFluentMigrator(this IHostApplicationBuilder builder, string connectionString)
    {
        builder.Services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb =>
                rb.AddMySql8()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations())
            .AddLogging(lb => lb.AddSerilog());
    }
}