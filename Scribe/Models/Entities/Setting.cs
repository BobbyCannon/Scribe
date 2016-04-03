#region References


using Speedy;

#endregion

namespace Scribe.Models.Entities
{
	public class Setting : ModifiableEntity
	{
		#region Properties

		public string Name { get; set; }

		public string Value { get; set; }

		#endregion
	}
}