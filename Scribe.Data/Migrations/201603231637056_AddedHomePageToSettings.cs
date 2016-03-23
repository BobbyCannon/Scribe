#region References

using System.Data.Entity.Migrations;

#endregion

namespace Scribe.Data.Migrations
{
	public partial class AddedHomePageToSettings : DbMigration
	{
		#region Methods

		public override void Down()
		{
			AddColumn("dbo.Pages", "IsHomePage", c => c.Boolean(false));
		}

		public override void Up()
		{
			DropColumn("dbo.Pages", "IsHomePage");
		}

		#endregion
	}
}