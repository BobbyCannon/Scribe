#region References

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;

#endregion

namespace Scribe.Models.Entities.Mapping
{
	public class PageMap : EntityTypeConfiguration<Page>
	{
		#region Constructors

		public PageMap()
		{
			// Primary Key
			HasKey(x => x.Id);

			// Table & Column Mappings
			ToTable("Pages");
			Property(x => x.Id).HasColumnName("Id").HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
			Property(x => x.CreatedOn).HasColumnName("CreatedOn").IsRequired().HasColumnType("datetime2").HasPrecision(7);
			Property(x => x.IsLocked).HasColumnName("IsLocked");
			Property(x => x.ModifiedOn).HasColumnName("ModifiedOn").IsRequired().HasColumnType("datetime2").HasPrecision(7);
			Property(x => x.Tags).HasColumnName("Tags").IsRequired().HasMaxLength(450);
			Property(x => x.Title).HasColumnName("Title").IsRequired().HasMaxLength(450).HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_Pages_Title") { IsUnique = true }));

			// Relationships
			HasRequired(x => x.CreatedBy)
				.WithMany(x => x.CreatedPages)
				.HasForeignKey(x => x.CreatedById)
				.WillCascadeOnDelete(false);
			HasRequired(x => x.ModifiedBy)
				.WithMany(x => x.ModifiedPages)
				.HasForeignKey(x => x.ModifiedById)
				.WillCascadeOnDelete(false);
			HasOptional(x => x.EditingBy)
				.WithMany(x => x.PagesBeingEdited)
				.HasForeignKey(x => x.EditingById)
				.WillCascadeOnDelete(false);
		}

		#endregion
	}
}