#region References

using System.Data.Entity.Migrations;

#endregion

namespace Scribe.Data.Migrations
{
	public partial class AddedPageStatus : DbMigration
	{
		#region Methods

		public override void Down()
		{
			AddColumn("dbo.Users", "Roles", c => c.String(false, 450));
			AlterColumn("dbo.Pages", "Text", c => c.String());
			DropColumn("dbo.PageHistory", "Title");
			DropColumn("dbo.PageHistory", "Tags");
			DropColumn("dbo.Pages", "Status");
			DropColumn("dbo.Users", "Tags");
		}

		public override void Up()
		{
			AddColumn("dbo.Users", "Tags", c => c.String(false, 450));
			AddColumn("dbo.Pages", "Status", c => c.Int(false));
			AddColumn("dbo.PageHistory", "Tags", c => c.String(false, 450));
			AddColumn("dbo.PageHistory", "Title", c => c.String(false, 450));
			AlterColumn("dbo.Pages", "Text", c => c.String(false));
			Sql("UPDATE [Users] SET [Tags] = [Roles]");
			DropColumn("dbo.Users", "Roles");
		}

		#endregion
	}
}