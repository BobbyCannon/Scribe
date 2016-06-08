#region References

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Speedy;

#endregion

namespace Scribe.Data.Entities
{
	public class Page : ModifiableEntity
	{
		#region Constructors

		[SuppressMessage("ReSharper", "VirtualMemberCallInContructor")]
		public Page()
		{
			Versions = new Collection<PageVersion>();
		}

		#endregion

		#region Properties

		public virtual PageVersion ApprovedVersion { get; set; }

		public virtual int? ApprovedVersionId { get; set; }

		public virtual PageVersion CurrentVersion { get; set; }

		public virtual int? CurrentVersionId { get; set; }

		/// <summary>
		/// Gets or sets a flag to indicated this pages has been "soft" deleted.
		/// </summary>
		public bool IsDeleted { get; set; }

		public virtual ICollection<PageVersion> Versions { get; set; }

		#endregion
	}
}