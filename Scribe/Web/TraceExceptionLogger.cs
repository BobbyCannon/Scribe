#region References

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Web.Http.ExceptionHandling;

#endregion

namespace Scribe.Web
{
	[ExcludeFromCodeCoverage]
	public class TraceExceptionLogger : ExceptionLogger
	{
		#region Methods

		public override void Log(ExceptionLoggerContext context)
		{
			Trace.TraceError(context.ExceptionContext.Exception.ToString());
		}

		#endregion
	}
}