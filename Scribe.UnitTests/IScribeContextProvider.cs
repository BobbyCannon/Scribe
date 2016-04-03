#region References

using Scribe.Data;

#endregion

namespace Scribe.UnitTests
{
	public interface IScribeContextProvider
	{
		#region Methods

		IScribeDatabase GetContext(bool clearDatabase = true);

		#endregion
	}
}