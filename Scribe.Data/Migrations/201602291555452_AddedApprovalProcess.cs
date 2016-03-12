#region References

#endregion

namespace Scribe.Data.Migrations
{
	public partial class AddedApprovalProcess : BaseDbMigration
	{
		#region Methods

		public override void Down()
		{
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
				.PrimaryKey(t => t.Id);

			AddColumn("dbo.Pages", "ModifiedById", c => c.Int(false));
			AddColumn("dbo.Pages", "IsLocked", c => c.Boolean(false));
			AddColumn("dbo.Users", "Roles", c => c.String(false, 450));
			DropForeignKey("dbo.Pages", "ParentId", "dbo.Pages");
			DropIndex("dbo.Pages", new[] { "ParentId" });
			AlterColumn("dbo.Pages", "Text", c => c.String());
			DropColumn("dbo.Settings", "ModifiedOn");
			DropColumn("dbo.Settings", "CreatedOn");
			DropColumn("dbo.Pages", "IsHomePage");
			DropColumn("dbo.Pages", "ParentId");
			DropColumn("dbo.Pages", "IsPublished");
			DropColumn("dbo.Pages", "IsDeleted");
			DropColumn("dbo.Pages", "ApprovalStatus");
			DropColumn("dbo.Users", "ModifiedOn");
			DropColumn("dbo.Users", "CreatedOn");
			DropColumn("dbo.Users", "Tags");
			DropColumn("dbo.Files", "IsDeleted");
			CreateIndex("dbo.PageHistory", "PageId");
			CreateIndex("dbo.PageHistory", "EditedById");
			CreateIndex("dbo.Pages", "Title", true, "IX_Pages_Title");
			CreateIndex("dbo.Pages", "ModifiedById");
			AddForeignKey("dbo.Pages", "ModifiedById", "dbo.Users", "Id");
			AddForeignKey("dbo.PageHistory", "PageId", "dbo.Pages", "Id", true);
			AddForeignKey("dbo.PageHistory", "EditedById", "dbo.Users", "Id");
		}

		public override void Up()
		{
			DropForeignKey("dbo.PageHistory", "EditedById", "dbo.Users");
			DropForeignKey("dbo.PageHistory", "PageId", "dbo.Pages");
			DropForeignKey("dbo.Pages", "ModifiedById", "dbo.Users");
			DropIndex("dbo.Pages", new[] { "ModifiedById" });
			DropIndex("dbo.Pages", "IX_Pages_Title");
			DropIndex("dbo.PageHistory", new[] { "EditedById" });
			DropIndex("dbo.PageHistory", new[] { "PageId" });
			AddColumn("dbo.Files", "IsDeleted", c => c.Boolean(false));
			AddColumn("dbo.Users", "Tags", c => c.String(false, 450));
			AddColumn("dbo.Users", "CreatedOn", c => c.DateTime(false, 7, storeType: "datetime2"));
			AddColumn("dbo.Users", "ModifiedOn", c => c.DateTime(false, 7, storeType: "datetime2"));
			AddColumn("dbo.Pages", "ApprovalStatus", c => c.Int(false));
			AddColumn("dbo.Pages", "IsDeleted", c => c.Boolean(false));
			AddColumn("dbo.Pages", "IsPublished", c => c.Boolean(false));
			AddColumn("dbo.Pages", "ParentId", c => c.Int());
			AddColumn("dbo.Pages", "IsHomePage", c => c.Boolean(false));
			AddColumn("dbo.Settings", "CreatedOn", c => c.DateTime(false, 7, storeType: "datetime2"));
			AddColumn("dbo.Settings", "ModifiedOn", c => c.DateTime(false, 7, storeType: "datetime2"));
			AlterColumn("dbo.Pages", "Text", c => c.String(false));
			CreateIndex("dbo.Pages", "ParentId");
			Sql("UPDATE [Pages] SET [ParentId] = [Id]");
			AddForeignKey("dbo.Pages", "ParentId", "dbo.Pages", "Id");
			Sql("UPDATE [Users] SET [Tags] = [Roles]");
			Sql("UPDATE [Users] SET Tags = REPLACE(Tags, ',Administrator,', ',administrator,')");
			DropColumn("dbo.Users", "Roles");
			DropColumn("dbo.Pages", "IsLocked");
			DropColumn("dbo.Pages", "ModifiedById");
			var historyMigration = @"
				-- Move only the 2 to N edits
				INSERT INTO [Pages] ([Title], [Tags], [CreatedOn], [ModifiedOn], [CreatedById], [EditingById], [EditingOn], [Text], [ApprovalStatus], [IsDeleted], [IsHomepage], [IsPublished], [ParentId])
				SELECT [Title], [Tags], [CreatedOn], [ModifiedOn], [CreatedById], [EditingById], [EditingOn], [Text], [ApprovalStatus], [IsDeleted], [IsHomepage], [IsPublished], [ParentId]
				FROM (
					SELECT ROW_NUMBER() OVER (PARTITION BY PageId ORDER BY h.EditedOn) as RowId, h.Id,
						p.Title, p.Tags, h.EditedOn as CreatedOn, h.EditedOn as ModifiedOn, h.EditedById as CreatedById, null as EditingById, 
						'1753-01-01' as EditingOn, h.Text, 0 as ApprovalStatus, 0 as IsDeleted, 0 as IsHomepage, 0 as IsPublished, h.PageId as ParentId
					FROM [PageHistory] h 
					JOIN [Pages] p ON p.Id = h.PageId
				) AS r
				WHERE r.RowId > 1
				ORDER BY CreatedOn

				-- Assign the first edit as the original page
				UPDATE p
				SET p.Title = r.Title, p.Tags = r.Tags, p.CreatedOn = r.CreatedOn, p.ModifiedOn = r.ModifiedOn, p.CreatedById = r.CreatedById, 
				p.EditingById = r.EditingById, p.EditingOn = r.EditingOn, p.Text = r.Text, p.ApprovalStatus = r.ApprovalStatus, p.IsDeleted = r.IsDeleted, 
				p.IsHomepage = r.IsHomepage, p.IsPublished = r.IsPublished, p.ParentId = r.ParentId
				FROM [Pages] p
				JOIN (
					SELECT ROW_NUMBER() OVER (PARTITION BY PageId ORDER BY h.EditedOn) as RowId, h.Id,
						p.Title, p.Tags, h.EditedOn as CreatedOn, h.EditedOn as ModifiedOn, h.EditedById as CreatedById, null as EditingById, 
						'1753-01-01' as EditingOn, h.Text, 0 as ApprovalStatus, 0 as IsDeleted, 0 as IsHomepage, 0 as IsPublished, h.PageId as ParentId
					FROM [PageHistory] h 
					JOIN [Pages] p ON p.Id = h.PageId
				) AS r ON r.ParentId = p.Id
				WHERE r.RowId = 1
				";
			Sql(historyMigration);
			DropTable("dbo.PageHistory");
			DropAllConstraints();
			Sql("CREATE INDEX [IX_Pages_IsDeleted] ON [Scribe].[dbo].[Pages] ([IsDeleted]) INCLUDE ([ParentId])");
			Sql("CREATE INDEX [IX_Pages_ApprovalStatus_IsPublished_IsDeleted] ON [Scribe].[dbo].[Pages] ([ApprovalStatus], [IsPublished], [IsDeleted]) INCLUDE ([ParentId])");
		}

		#endregion
	}
}