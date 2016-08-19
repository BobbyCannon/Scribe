#region References

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using System.Diagnostics.CodeAnalysis;
using Scribe.Data.Entities;

#endregion

namespace Scribe.Data.Mapping
{
	[ExcludeFromCodeCoverage]
	public class SettingsMap : EntityTypeConfiguration<Setting>
	{
		#region Constructors

		public SettingsMap()
		{
			// Primary Key
			HasKey(x => x.Id);

			// Table & Column Mappings
			ToTable("Settings");
			Property(x => x.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
			Property(x => x.CreatedOn).IsRequired().HasColumnType("datetime2").HasPrecision(7);
			Property(x => x.ModifiedOn).IsRequired().HasColumnType("datetime2").HasPrecision(7);
			Property(x => x.Name).IsRequired().HasMaxLength(450).HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_Settings_Name_Type_UserId") { IsUnique = true, Order = 1 }));
			Property(x => x.Type).IsRequired().HasMaxLength(450).HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_Settings_Name_Type_UserId") { IsUnique = true, Order = 2 }));
			Property(x => x.UserId).IsOptional().HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_Settings_Name_Type_UserId") { IsUnique = true, Order = 3 }));
			Property(x => x.Value).IsRequired();

			// Relationships
			HasOptional(x => x.User)
				.WithMany(x => x.Settings)
				.HasForeignKey(x => x.UserId)
				.WillCascadeOnDelete(true);
		}

		#endregion
	}
}