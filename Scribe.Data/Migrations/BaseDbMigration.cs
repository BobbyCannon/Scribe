#region References

using System.Data.Entity.Migrations;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace Scribe.Data.Migrations
{
	[ExcludeFromCodeCoverage]
	public abstract class BaseDbMigration : DbMigration
	{
		#region Methods

		protected void AddDefaultConstraint(string table, string column, string constraint)
		{
			Sql("ALTER TABLE [dbo].[" + table + "] ADD DEFAULT (" + constraint + ") FOR [" + column + "]");
		}

		protected void DropAllConstraints()
		{
			Sql("DECLARE @query AS VARCHAR(MAX) = ''; SELECT @query += 'ALTER TABLE [dbo].[' + p.name + '] DROP CONSTRAINT ['+ dc.name + '];' + CHAR(10) FROM sys.default_constraints dc JOIN sys.objects p ON p.object_id = dc.parent_object_id; EXEC (@query);");
		}

		protected void DropConstraint(string table, string column)
		{
			Sql(@"DECLARE @con nvarchar(128) SELECT @con = name FROM sys.default_constraints WHERE parent_object_id = object_id('dbo." + table + "') AND col_name(parent_object_id, parent_column_id) = '" + column + "'; IF @con IS NOT NULL EXECUTE('ALTER TABLE [dbo].[" + table + "] DROP CONSTRAINT ' + @con)");
		}

		#endregion
	}
}