#region References

using System.Data.Entity.Migrations;

#endregion

namespace Scribe.Data.Migrations
{
	public partial class AddedUserSettings : DbMigration
	{
		#region Methods

		public override void Down()
		{
			DropForeignKey("dbo.Settings", "UserId", "dbo.Users");
			DropIndex("dbo.Settings", "IX_Settings_Name_Type_UserId");
			DropColumn("dbo.Settings", "SyncId");
			DropColumn("dbo.Settings", "UserId");
			DropColumn("dbo.Settings", "Type");
			DropColumn("dbo.Users", "PictureUrl");
			CreateIndex("dbo.Settings", "Name", true, "IX_Settings_Name");
		}

		public override void Up()
		{
			DropIndex("dbo.Settings", "IX_Settings_Name");
			AddColumn("dbo.Users", "PictureUrl", c => c.String());
			AddColumn("dbo.Settings", "Type", c => c.String(false, 450));
			AddColumn("dbo.Settings", "UserId", c => c.Int());
			AddColumn("dbo.Settings", "SyncId", c => c.Guid(false));
			CreateIndex("dbo.Settings", new[] { "Name", "Type", "UserId" }, true, "IX_Settings_Name_Type_UserId");
			AddForeignKey("dbo.Settings", "UserId", "dbo.Users", "Id", true);
			Sql("UPDATE [Settings] SET [Type] = 'Scribe.Website.Services.Settings.SiteSettings,Scribe.Website', [Name] = REPLACE([Name], ' ', '')");
			Sql("UPDATE [Settings] SET [Name] = 'FrontPagePrivateId' WHERE[Name] = 'PrivateFrontPage'");
			Sql("UPDATE [Settings] SET [Name] = 'FrontPagePublicId' WHERE[Name] = 'PublicFrontPage'");
		}

		#endregion
	}
}