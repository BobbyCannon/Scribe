#region References

using System;

#endregion

namespace Scribe.Data
{
	public class ScribeDatabaseProvider
	{
		#region Fields

		private readonly Func<IScribeDatabase> _provider;

		#endregion

		#region Constructors

		public ScribeDatabaseProvider(Func<IScribeDatabase> provider)
		{
			_provider = provider;
		}

		#endregion

		#region Methods

		public IScribeDatabase GetDatabase()
		{
			return _provider();
		}

		#endregion
	}
}