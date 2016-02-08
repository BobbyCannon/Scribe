#region References

using System.Data.Entity.Migrations;

#endregion

namespace Scribe.Migrations
{
	public partial class UpdatedToEasyDataFramework : DbMigration
	{
		#region Methods

		public override void Down()
		{
			DropColumn("dbo.Settings", "ModifiedOn");
			DropColumn("dbo.Settings", "CreatedOn");
			DropColumn("dbo.PageHistory", "ModifiedOn");
			DropColumn("dbo.PageHistory", "CreatedOn");
			DropColumn("dbo.Users", "ModifiedOn");
			DropColumn("dbo.Users", "CreatedOn");
		}

		public override void Up()
		{
			AddColumn("dbo.Users", "CreatedOn", c => c.DateTime(false, 7, storeType: "datetime2"));
			AddColumn("dbo.Users", "ModifiedOn", c => c.DateTime(false, 7, storeType: "datetime2"));
			AddColumn("dbo.PageHistory", "CreatedOn", c => c.DateTime(false, 7, storeType: "datetime2"));
			AddColumn("dbo.PageHistory", "ModifiedOn", c => c.DateTime(false, 7, storeType: "datetime2"));
			AddColumn("dbo.Settings", "CreatedOn", c => c.DateTime(false, 7, storeType: "datetime2"));
			AddColumn("dbo.Settings", "ModifiedOn", c => c.DateTime(false, 7, storeType: "datetime2"));
		}

		#endregion
	}
}