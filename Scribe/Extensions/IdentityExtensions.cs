#region References

using System.Linq;
using System.Security.Principal;

#endregion

namespace Scribe.Extensions
{
	public static class IdentityExtensions
	{
		#region Methods

		public static string GetDisplayName(this IIdentity identity)
		{
			return !identity.Name.Contains(';') ? string.Empty : identity.Name.Split(';').Last();
		}

		public static int GetId(this IIdentity identity)
		{
			return !identity.Name.Contains(';') ? 0 : identity.Name.Split(';').First().ConvertToInt();
		}

		#endregion
	}
}