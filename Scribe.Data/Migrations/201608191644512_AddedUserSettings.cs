namespace Scribe.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddedUserSettings : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Settings", "IX_Settings_Name");
            AddColumn("dbo.Users", "PictureUrl", c => c.String());
            AddColumn("dbo.Settings", "Type", c => c.String(nullable: false, maxLength: 450));
            AddColumn("dbo.Settings", "UserId", c => c.Int());
            AddColumn("dbo.Settings", "SyncId", c => c.Guid(nullable: false));
            CreateIndex("dbo.Settings", new[] { "Name", "Type", "UserId" }, unique: true, name: "IX_Settings_Name_Type_UserId");
            AddForeignKey("dbo.Settings", "UserId", "dbo.Users", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Settings", "UserId", "dbo.Users");
            DropIndex("dbo.Settings", "IX_Settings_Name_Type_UserId");
            DropColumn("dbo.Settings", "SyncId");
            DropColumn("dbo.Settings", "UserId");
            DropColumn("dbo.Settings", "Type");
            DropColumn("dbo.Users", "PictureUrl");
            CreateIndex("dbo.Settings", "Name", unique: true, name: "IX_Settings_Name");
        }
    }
}
