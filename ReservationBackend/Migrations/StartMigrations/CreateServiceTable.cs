using FluentMigrator;

namespace ReservationBackend.Migrations;

[Migration(202609080001)]
public class CreateServiceTable : Migration
{
    public override void Up()
    {
        Create.Table("Services")
            .AddBaseDomainEntityColumns()
            .WithColumn("Name").AsString(255).NotNullable()
            .WithColumn("DurationMinutes").AsInt32().NotNullable();

        Create.Index("IX_Services_Name")
            .OnTable("Services")
            .OnColumn("Name");
    }

    public override void Down()
    {
        Delete.Index("IX_Services_Name").OnTable("Services");
        Delete.Table("Services");
    }
}