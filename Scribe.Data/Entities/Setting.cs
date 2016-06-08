#region References

using Speedy;

#endregion

namespace Scribe.Data.Entities
{
	public class Setting : ModifiableEntity
	{
		#region Properties

		public string Name { get; set; }

		public string Value { get; set; }

		#endregion
	}
}