#region References

using EasyDataFramework;

#endregion

namespace Scribe.Models.Entities
{
	public class Setting : Entity
	{
		#region Properties

		public string Name { get; set; }

		public string Value { get; set; }

		#endregion
	}
}