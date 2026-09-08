using System.Data;
using FluentMigrator;

namespace ReservationBackend.Migrations;

[Migration(202609080003)]
public class CreateReservationTable : Migration
{
    public override void Up()
    {
        Create.Table("Reservations")
            .AddBaseDomainEntityColumns()
            .WithColumn("SlotId").AsGuid().NotNullable()
            .WithColumn("Name").AsString(255).NotNullable()
            .WithColumn("PhoneNumber").AsString(32).NotNullable();

        Create.ForeignKey("FK_Reservations_Slots")
            .FromTable("Reservations").ForeignColumn("SlotId")
            .ToTable("Slots").PrimaryColumn("Id")
            .OnDelete(Rule.None);

        Create.Index("UX_Reservations_SlotId")
            .OnTable("Reservations")
            .OnColumn("SlotId")
            .Unique();
    }

    public override void Down()
    {
        Delete.Index("UX_Reservations_SlotId").OnTable("Reservations");
        Delete.ForeignKey("FK_Reservations_Slots").OnTable("Reservations");
        Delete.Table("Reservations");
    }
}