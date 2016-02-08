namespace Scribe.Migrations
{
	public partial class AddedDeleteFlag : BaseDbMigration
	{
		#region Methods

		public override void Down()
		{
			DropColumn("dbo.Pages", "IsDeleted");
			DropColumn("dbo.Files", "IsDeleted");
		}

		public override void Up()
		{
			AddColumn("dbo.Files", "IsDeleted", c => c.Boolean(false));
			AddColumn("dbo.Pages", "IsDeleted", c => c.Boolean(false));
			DropAllConstraints();
		}

		#endregion
	}
}