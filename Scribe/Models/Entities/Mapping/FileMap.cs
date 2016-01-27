#region References

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;

#endregion

namespace Scribe.Models.Entities.Mapping
{
	public class FileMap : EntityTypeConfiguration<File>
	{
		#region Constructors

		public FileMap()
		{
			// Primary Key
			HasKey(x => x.Id);

			// Table & Column Mappings
			ToTable("Files");
			Property(x => x.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
			Property(x => x.CreatedOn).HasColumnName("CreatedOn").IsRequired().HasColumnType("datetime2").HasPrecision(7);
			Property(x => x.Data).IsRequired();
			Property(x => x.ModifiedOn).HasColumnName("ModifiedOn").IsRequired().HasColumnType("datetime2").HasPrecision(7);
			Property(x => x.Name).IsRequired().HasMaxLength(450).HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_Files_Name") { IsUnique = true }));
			Property(x => x.Size).IsRequired();
			Property(x => x.Type).IsRequired().HasMaxLength(450);

			// Relationships
			HasRequired(x => x.CreatedBy)
				.WithMany(x => x.CreatedFiles)
				.HasForeignKey(x => x.CreatedById)
				.WillCascadeOnDelete(false);
			HasRequired(x => x.ModifiedBy)
				.WithMany(x => x.ModifiedFiles)
				.HasForeignKey(x => x.ModifiedById)
				.WillCascadeOnDelete(false);
		}

		#endregion
	}
}