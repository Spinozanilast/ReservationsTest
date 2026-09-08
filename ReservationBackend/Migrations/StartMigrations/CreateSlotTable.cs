using System.Data;
using FluentMigrator;

namespace ReservationBackend.Migrations;

[Migration(202609080002)]
public class CreateSlotTable : Migration
{
    public override void Up()
    {
        Create.Table("Slots")
            .AddBaseDomainEntityColumns()
            .WithColumn("ServiceId").AsGuid().NotNullable()
            .WithColumn("SlotStartTime").AsCustom("datetime(3)").NotNullable();

        Create.ForeignKey("FK_Slots_Services")
            .FromTable("Slots").ForeignColumn("ServiceId")
            .ToTable("Services").PrimaryColumn("Id")
            .OnDelete(Rule.None);

        Create.Index("UX_Slots_ServiceId_SlotStartTime")
            .OnTable("Slots")
            .OnColumn("ServiceId").Ascending()
            .OnColumn("SlotStartTime").Ascending()
            .WithOptions().Unique();

        Create.Index("IX_Slots_SlotStartTime")
            .OnTable("Slots")
            .OnColumn("SlotStartTime");
    }

    public override void Down()
    {
        Delete.Index("UX_Slots_ServiceId_SlotStartTime").OnTable("Slots");
        Delete.Index("IX_Slots_SlotStartTime").OnTable("Slots");
        Delete.ForeignKey("FK_Slots_Services").OnTable("Slots");
        Delete.Table("Slots");
    }
}