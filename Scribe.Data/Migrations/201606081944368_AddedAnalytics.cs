#region References

using System.Data.Entity.Migrations;

#endregion

namespace Scribe.Data.Migrations
{
	public partial class AddedAnalytics : DbMigration
	{
		#region Methods

		public override void Down()
		{
			DropForeignKey("dbo.EventValues", "EventId", "dbo.Events");
			DropForeignKey("dbo.Events", "ParentId", "dbo.Events");
			DropIndex("dbo.EventValues", "IX_EventValues_EventId_Name");
			DropIndex("dbo.Events", "IX_Events_UniqueId");
			DropIndex("dbo.Events", new[] { "ParentId" });
			DropIndex("dbo.Events", "IX_Events_Name");
			DropTable("dbo.EventValues");
			DropTable("dbo.Events");
		}

		public override void Up()
		{
			CreateTable(
				"dbo.Events",
				c => new
				{
					Id = c.Int(false, true),
					CompletedOn = c.DateTime(false, 7, storeType: "datetime2"),
					ElapsedTicks = c.Long(false),
					Name = c.String(false, 900, unicode: false),
					ParentId = c.Int(),
					SessionId = c.Guid(false),
					StartedOn = c.DateTime(false, 7, storeType: "datetime2"),
					Type = c.Int(false),
					UniqueId = c.Guid(false),
					CreatedOn = c.DateTime(false, 7, storeType: "datetime2")
				})
				.PrimaryKey(t => t.Id)
				.ForeignKey("dbo.Events", t => t.ParentId)
				.Index(t => t.Name, "IX_Events_Name")
				.Index(t => t.ParentId)
				.Index(t => t.UniqueId, unique: true, name: "IX_Events_UniqueId");

			CreateTable(
				"dbo.EventValues",
				c => new
				{
					Id = c.Int(false, true),
					EventId = c.Int(false),
					Name = c.String(false, 896, unicode: false),
					Value = c.String(false, unicode: false),
					CreatedOn = c.DateTime(false, 7, storeType: "datetime2")
				})
				.PrimaryKey(t => t.Id)
				.ForeignKey("dbo.Events", t => t.EventId, true)
				.Index(t => new { t.EventId, t.Name }, unique: true, name: "IX_EventValues_EventId_Name");
		}

		#endregion
	}
}