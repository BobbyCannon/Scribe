#region References

using System.Data.Entity.Migrations;

#endregion

namespace Scribe.Migrations
{
	public partial class InitialMigration : DbMigration
	{
		#region Methods

		public override void Down()
		{
			DropForeignKey("dbo.Files", "ModifiedById", "dbo.Users");
			DropForeignKey("dbo.Files", "CreatedById", "dbo.Users");
			DropForeignKey("dbo.Pages", "ModifiedById", "dbo.Users");
			DropForeignKey("dbo.PageHistory", "PageId", "dbo.Pages");
			DropForeignKey("dbo.PageHistory", "EditedById", "dbo.Users");
			DropForeignKey("dbo.Pages", "EditingById", "dbo.Users");
			DropForeignKey("dbo.Pages", "CreatedById", "dbo.Users");
			DropIndex("dbo.Settings", "IX_Settings_Name");
			DropIndex("dbo.PageHistory", new[] { "PageId" });
			DropIndex("dbo.PageHistory", new[] { "EditedById" });
			DropIndex("dbo.Pages", "IX_Pages_Title");
			DropIndex("dbo.Pages", new[] { "ModifiedById" });
			DropIndex("dbo.Pages", new[] { "EditingById" });
			DropIndex("dbo.Pages", new[] { "CreatedById" });
			DropIndex("dbo.Users", "IX_Users_UserName");
			DropIndex("dbo.Users", "IX_Users_EmailAddress");
			DropIndex("dbo.Users", "IX_Users_DisplayName");
			DropIndex("dbo.Files", "IX_Files_Name");
			DropIndex("dbo.Files", new[] { "ModifiedById" });
			DropIndex("dbo.Files", new[] { "CreatedById" });
			DropTable("dbo.Settings");
			DropTable("dbo.PageHistory");
			DropTable("dbo.Pages");
			DropTable("dbo.Users");
			DropTable("dbo.Files");
		}

		public override void Up()
		{
			CreateTable(
				"dbo.Files",
				c => new
				{
					Id = c.Int(false, true),
					CreatedById = c.Int(false),
					CreatedOn = c.DateTime(false, 7, storeType: "datetime2"),
					Data = c.Binary(false),
					ModifiedById = c.Int(false),
					ModifiedOn = c.DateTime(false, 7, storeType: "datetime2"),
					Name = c.String(false, 450),
					Size = c.Int(false),
					Type = c.String(false, 450)
				})
				.PrimaryKey(t => t.Id)
				.ForeignKey("dbo.Users", t => t.CreatedById)
				.ForeignKey("dbo.Users", t => t.ModifiedById)
				.Index(t => t.CreatedById)
				.Index(t => t.ModifiedById)
				.Index(t => t.Name, unique: true, name: "IX_Files_Name");

			CreateTable(
				"dbo.Users",
				c => new
				{
					Id = c.Int(false, true),
					DisplayName = c.String(false, 450),
					EmailAddress = c.String(false, 450),
					IsActiveDirectory = c.Boolean(false),
					IsEnabled = c.Boolean(false),
					PasswordHash = c.String(false, 450),
					Roles = c.String(false, 450),
					Salt = c.String(false, 128),
					UserName = c.String(false, 256)
				})
				.PrimaryKey(t => t.Id)
				.Index(t => t.DisplayName, unique: true, name: "IX_Users_DisplayName")
				.Index(t => t.EmailAddress, unique: true, name: "IX_Users_EmailAddress")
				.Index(t => t.UserName, unique: true, name: "IX_Users_UserName");

			CreateTable(
				"dbo.Pages",
				c => new
				{
					Id = c.Int(false, true),
					CreatedById = c.Int(false),
					CreatedOn = c.DateTime(false, 7, storeType: "datetime2"),
					EditingById = c.Int(),
					EditingOn = c.DateTime(false),
					IsLocked = c.Boolean(false),
					ModifiedById = c.Int(false),
					ModifiedOn = c.DateTime(false, 7, storeType: "datetime2"),
					Tags = c.String(false, 450),
					Text = c.String(),
					Title = c.String(false, 450)
				})
				.PrimaryKey(t => t.Id)
				.ForeignKey("dbo.Users", t => t.CreatedById)
				.ForeignKey("dbo.Users", t => t.EditingById)
				.ForeignKey("dbo.Users", t => t.ModifiedById)
				.Index(t => t.CreatedById)
				.Index(t => t.EditingById)
				.Index(t => t.ModifiedById)
				.Index(t => t.Title, unique: true, name: "IX_Pages_Title");

			CreateTable(
				"dbo.PageHistory",
				c => new
				{
					Id = c.Int(false, true),
					EditedById = c.Int(false),
					EditedOn = c.DateTime(false, 7, storeType: "datetime2"),
					PageId = c.Int(false),
					Text = c.String(false)
				})
				.PrimaryKey(t => t.Id)
				.ForeignKey("dbo.Users", t => t.EditedById)
				.ForeignKey("dbo.Pages", t => t.PageId, true)
				.Index(t => t.EditedById)
				.Index(t => t.PageId);

			CreateTable(
				"dbo.Settings",
				c => new
				{
					Id = c.Int(false, true),
					Name = c.String(false, 450),
					Value = c.String(false)
				})
				.PrimaryKey(t => t.Id)
				.Index(t => t.Name, unique: true, name: "IX_Settings_Name");
		}

		#endregion
	}
}