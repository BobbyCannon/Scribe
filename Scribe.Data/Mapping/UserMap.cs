#region References

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using Scribe.Models.Entities;

#endregion

namespace Scribe.Data.Mapping
{
	public class UserMap : EntityTypeConfiguration<User>
	{
		#region Constructors

		public UserMap()
		{
			// Primary Key
			HasKey(x => x.Id);

			// Table & Column Mappings
			ToTable("Users");
			Property(x => x.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
			Property(x => x.CreatedOn).IsRequired().HasColumnType("datetime2").HasPrecision(7);
			Property(x => x.DisplayName).IsRequired().HasMaxLength(450).HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_Users_DisplayName") { IsUnique = true }));
			Property(x => x.EmailAddress).IsRequired().HasMaxLength(450).HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_Users_EmailAddress") { IsUnique = true }));
			Property(x => x.IsActiveDirectory).IsRequired();
			Property(x => x.IsEnabled).IsRequired();
			Property(x => x.ModifiedOn).IsRequired().HasColumnType("datetime2").HasPrecision(7);
			Property(x => x.PasswordHash).IsRequired().HasMaxLength(450);
			Property(x => x.Salt).IsRequired().HasMaxLength(128);
			Property(x => x.Tags).IsRequired().HasMaxLength(450);
			Property(x => x.UserName).IsRequired().HasMaxLength(256).HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_Users_UserName") { IsUnique = true }));
		}

		#endregion
	}
}