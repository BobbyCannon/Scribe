#region References

using System.Data.Entity.Migrations;

#endregion

namespace Scribe.Data.Migrations
{
	public partial class AddedPageVersions : DbMigration
	{
		#region Methods

		public override void Down()
		{
			AddColumn("dbo.PageVersions", "IsHomePage", c => c.Boolean(false));
			AddColumn("dbo.PageVersions", "ParentId", c => c.Int());
			AddColumn("dbo.PageVersions", "IsDeleted", c => c.Boolean(false));
			DropForeignKey("dbo.PageVersions", "PageId", "dbo.Pages");
			DropForeignKey("dbo.Pages", "CurrentVersionId", "dbo.PageVersions");
			DropForeignKey("dbo.Pages", "ApprovedVersionId", "dbo.PageVersions");
			DropIndex("dbo.Pages", new[] { "CurrentVersionId" });
			DropIndex("dbo.Pages", new[] { "ApprovedVersionId" });
			DropIndex("dbo.PageVersions", new[] { "PageId" });
			DropColumn("dbo.PageVersions", "PageId");
			DropTable("dbo.Pages");
			CreateIndex("dbo.PageVersions", "ParentId");
			AddForeignKey("dbo.PageVersions", "ParentId", "dbo.PageVersions", "Id");
			RenameTable("dbo.PageVersions", "Pages");
		}

		public override void Up()
		{
			RenameTable("dbo.Pages", "PageVersions");
			DropForeignKey("dbo.PageVersions", "ParentId", "dbo.PageVersions");
			DropIndex("dbo.PageVersions", new[] { "ParentId" });
			CreateTable(
				"dbo.Pages",
				c => new
				{
					Id = c.Int(false, true),
					IsHomePage = c.Boolean(false),
					ApprovedVersionId = c.Int(),
					CurrentVersionId = c.Int(),
					IsDeleted = c.Boolean(false),
					CreatedOn = c.DateTime(false, 7, storeType: "datetime2"),
					ModifiedOn = c.DateTime(false, 7, storeType: "datetime2")
				})
				.PrimaryKey(t => t.Id)
				.ForeignKey("dbo.PageVersions", t => t.ApprovedVersionId)
				.ForeignKey("dbo.PageVersions", t => t.CurrentVersionId)
				.Index(t => t.ApprovedVersionId)
				.Index(t => t.CurrentVersionId);

			AddColumn("dbo.PageVersions", "PageId", c => c.Int(false));

			Sql("SET IDENTITY_INSERT [Pages] ON; INSERT INTO [Pages] ([Id], [CreatedOn], [ModifiedOn], [IsDeleted], [IsHomePage]) SELECT [Id], [CreatedOn], [ModifiedOn], 0, 0 FROM [PageVersions] WHERE [Id] = [ParentId]; SET IDENTITY_INSERT [Pages] OFF;");
			Sql("UPDATE [PageVersions] SET [PageId] = [ParentId]");
			Sql("DROP INDEX [IX_Pages_IsDeleted] ON [dbo].[PageVersions]");
			Sql("DROP INDEX [IX_Pages_ApprovalStatus_IsPublished_IsDeleted] ON [Scribe].[dbo].[PageVersions]");
			Sql("ALTER TABLE dbo.PageVersions DROP CONSTRAINT [FK_dbo.Pages_dbo.Pages_ParentId]");
			Sql("UPDATE p SET [CurrentVersionId] = (SELECT TOP 1 [Id] FROM [PageVersions] WHERE [PageId] = p.[Id] ORDER BY [Id] DESC) FROM [Pages] p");
			Sql("UPDATE p SET [ApprovedVersionId] = (SELECT TOP 1 [Id] FROM [PageVersions] WHERE [PageId] = p.[Id] AND [ApprovalStatus] = 2 AND [IsPublished] = 1 ORDER BY [Id] DESC) FROM [Pages] p");

			CreateIndex("dbo.PageVersions", "PageId");
			AddForeignKey("dbo.PageVersions", "PageId", "dbo.Pages", "Id");
			DropColumn("dbo.PageVersions", "IsDeleted");
			DropColumn("dbo.PageVersions", "ParentId");
			DropColumn("dbo.PageVersions", "IsHomePage");
		}

		#endregion
	}
}