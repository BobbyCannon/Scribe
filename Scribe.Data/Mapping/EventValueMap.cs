#region References

using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using Scribe.Data.Entities;

#endregion

namespace Scribe.Data.Mapping
{
	public class EventValueMap : EntityTypeConfiguration<EventValue>
	{
		#region Constructors

		public EventValueMap()
		{
			// Primary Key
			HasKey(x => x.Id);

			// Table & Column Mappings
			ToTable("EventValues");
			Property(x => x.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
			Property(x => x.CreatedOn).HasColumnType("DateTime2").IsRequired();
			Property(x => x.EventId)
				.HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_EventValues_EventId_Name") { IsUnique = true, Order = 1 }));
			Property(x => x.Name).HasMaxLength(900 - 4)
				.IsRequired()
				.HasColumnType("VarChar")
				.HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_EventValues_Name") { IsUnique = false }))
				.HasColumnAnnotation("Index", new IndexAnnotation(new IndexAttribute("IX_EventValues_EventId_Name") { IsUnique = true, Order = 2 }));
			Property(x => x.Value).HasColumnType("VarChar(MAX)").IsRequired();

			// Relationship
			HasRequired(x => x.Event)
				.WithMany(x => x.Values)
				.HasForeignKey(x => x.EventId);
		}

		#endregion
	}
}