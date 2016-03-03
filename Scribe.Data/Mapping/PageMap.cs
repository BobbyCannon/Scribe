#region References

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Diagnostics.CodeAnalysis;
using Scribe.Models.Entities;

#endregion

namespace Scribe.Data.Mapping
{
	[ExcludeFromCodeCoverage]
	public class PageMap : EntityTypeConfiguration<Page>
	{
		#region Constructors

		public PageMap()
		{
			// Primary Key
			HasKey(x => x.Id);

			// Table & Column Mappings
			ToTable("Pages");
			Property(x => x.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
			Property(x => x.CreatedOn).IsRequired().HasColumnType("datetime2").HasPrecision(7);
			Property(x => x.IsDeleted).IsRequired();
			Property(x => x.IsHomePage).IsRequired();
			Property(x => x.ModifiedOn).IsRequired().HasColumnType("datetime2").HasPrecision(7);

			// Relationships
			HasOptional(x => x.CurrentVersion)
				.WithMany()
				.HasForeignKey(x => x.CurrentVersionId)
				.WillCascadeOnDelete(false);
			HasOptional(x => x.ApprovedVersion)
				.WithMany()
				.HasForeignKey(x => x.ApprovedVersionId)
				.WillCascadeOnDelete(false);
		}

		#endregion
	}
}