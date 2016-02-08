#region References

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

#endregion

namespace Scribe.Models.Entities.Mapping
{
	public class PageHistoryMap : EntityTypeConfiguration<PageHistory>
	{
		#region Constructors

		public PageHistoryMap()
		{
			// Primary Key
			HasKey(x => x.Id);

			// Table & Column Mappings
			ToTable("PageHistory");
			Property(x => x.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
			Property(x => x.CreatedOn).IsRequired().HasColumnType("datetime2").HasPrecision(7);
			Property(x => x.EditedOn).IsRequired().HasColumnType("datetime2").HasPrecision(7);
			Property(x => x.ModifiedOn).IsRequired().HasColumnType("datetime2").HasPrecision(7);
			Property(x => x.Text).IsRequired();

			// Relationships
			HasRequired(x => x.Page)
				.WithMany(x => x.History)
				.HasForeignKey(d => d.PageId)
				.WillCascadeOnDelete(true);
			HasRequired(x => x.EditedBy)
				.WithMany(x => x.PageHistories)
				.HasForeignKey(x => x.EditedById)
				.WillCascadeOnDelete(false);
		}

		#endregion
	}
}