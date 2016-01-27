#region References

using System;
using System.Runtime.Serialization;

#endregion

namespace Scribe.Exceptions
{
	[Serializable]
	public class PageNotFoundException : Exception
	{
		#region Constructors

		public PageNotFoundException()
		{
		}

		public PageNotFoundException(string message) : base(message)
		{
		}

		public PageNotFoundException(string message, Exception inner) : base(message, inner)
		{
		}

		protected PageNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		#endregion
	}
}