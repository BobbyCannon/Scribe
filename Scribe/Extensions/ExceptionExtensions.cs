#region References

using System;
using System.Text;

#endregion

namespace Scribe.Extensions
{
	public static class ExceptionExtensions
	{
		#region Methods

		public static string CleanMessage(this Exception ex)
		{
			var offset = ex.Message.IndexOf("\r\nParameter");
			return offset > 0 ? ex.Message.Substring(0, offset) : ex.Message;
		}

		public static string ToDetailedString(this Exception ex)
		{
			var builder = new StringBuilder();
			AddExceptionToBuilder(builder, ex);
			return builder.ToString();
		}

		private static void AddExceptionToBuilder(StringBuilder builder, Exception ex)
		{
			builder.Append(builder.Length > 0 ? "\r\n" + ex.Message : ex.Message);

			if (ex.InnerException != null)
			{
				AddExceptionToBuilder(builder, ex.InnerException);
			}
		}

		#endregion
	}
}