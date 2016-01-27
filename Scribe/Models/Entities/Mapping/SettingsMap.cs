#region References

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;

#endregion

namespace Scribe.Models.Entities.Mapping
{
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
			Property(x => x.Name).IsRequired().HasMaxLength(450).HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_Settings_Name") { IsUnique = true }));
			Property(x => x.Value).IsRequired();
		}

		#endregion
	}
}