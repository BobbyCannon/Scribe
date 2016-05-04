#region References

using System.Diagnostics.CodeAnalysis;
using Microsoft.Owin;
using Owin;
using Scribe.Website;

#endregion

[assembly: OwinStartup(typeof(Startup))]

namespace Scribe.Website
{
	[ExcludeFromCodeCoverage]
	public class Startup
	{
		#region Methods

		public void Configuration(IAppBuilder app)
		{
			app.MapSignalR();
		}

		#endregion
	}
}