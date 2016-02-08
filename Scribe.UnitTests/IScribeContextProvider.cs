#region References

using Scribe.Data;

#endregion

namespace Scribe.UnitTests
{
	public interface IScribeContextProvider
	{
		#region Methods

		IScribeContext GetContext(bool clearDatabase = true);

		#endregion
	}
}