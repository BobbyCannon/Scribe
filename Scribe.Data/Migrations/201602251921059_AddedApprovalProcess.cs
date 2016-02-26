namespace Scribe.Data.Migrations
{
	public partial class AddedApprovalProcess : BaseDbMigration
	{
		#region Methods

		public override void Down()
		{
			AddColumn("dbo.Pages", "IsLocked", c => c.Boolean(false));
			AddColumn("dbo.Users", "Roles", c => c.String(false, 450));
			AlterColumn("dbo.Pages", "Text", c => c.String());
			DropColumn("dbo.Settings", "ModifiedOn");
			DropColumn("dbo.Settings", "CreatedOn");
			DropColumn("dbo.PageHistory", "ModifiedOn");
			DropColumn("dbo.PageHistory", "CreatedOn");
			DropColumn("dbo.PageHistory", "Title");
			DropColumn("dbo.PageHistory", "Tags");
			DropColumn("dbo.PageHistory", "ApprovalStatus");
			DropColumn("dbo.Pages", "IsPublished");
			DropColumn("dbo.Pages", "IsDeleted");
			DropColumn("dbo.Pages", "ApprovalStatus");
			DropColumn("dbo.Users", "ModifiedOn");
			DropColumn("dbo.Users", "CreatedOn");
			DropColumn("dbo.Users", "Tags");
			DropColumn("dbo.Files", "IsDeleted");
		}

		public override void Up()
		{
			AddColumn("dbo.Files", "IsDeleted", c => c.Boolean(false));
			AddColumn("dbo.Users", "Tags", c => c.String(false, 450));
			AddColumn("dbo.Users", "CreatedOn", c => c.DateTime(false, 7, storeType: "datetime2"));
			AddColumn("dbo.Users", "ModifiedOn", c => c.DateTime(false, 7, storeType: "datetime2"));
			AddColumn("dbo.Pages", "ApprovalStatus", c => c.Int(false));
			AddColumn("dbo.Pages", "IsDeleted", c => c.Boolean(false));
			AddColumn("dbo.Pages", "IsPublished", c => c.Boolean(false));
			AddColumn("dbo.PageHistory", "ApprovalStatus", c => c.Int(false));
			AddColumn("dbo.PageHistory", "Tags", c => c.String(false, 450));
			AddColumn("dbo.PageHistory", "Title", c => c.String(false, 450));
			AddColumn("dbo.PageHistory", "CreatedOn", c => c.DateTime(false, 7, storeType: "datetime2"));
			AddColumn("dbo.PageHistory", "ModifiedOn", c => c.DateTime(false, 7, storeType: "datetime2"));
			AddColumn("dbo.Settings", "CreatedOn", c => c.DateTime(false, 7, storeType: "datetime2"));
			AddColumn("dbo.Settings", "ModifiedOn", c => c.DateTime(false, 7, storeType: "datetime2"));
			AlterColumn("dbo.Pages", "Text", c => c.String(false));
			Sql("UPDATE [Users] SET [Tags] = [Roles]");
			DropColumn("dbo.Users", "Roles");
			DropColumn("dbo.Pages", "IsLocked");
			DropAllConstraints();
		}

		#endregion
	}
}