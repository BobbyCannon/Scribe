#region References

using System;
using System.Runtime.Serialization;

#endregion

namespace Scribe.Exceptions
{
	[Serializable]
	public class UserNotFoundException : Exception
	{
		#region Constructors

		public UserNotFoundException()
		{
		}

		public UserNotFoundException(string message) : base(message)
		{
		}

		public UserNotFoundException(string message, Exception inner) : base(message, inner)
		{
		}

		protected UserNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		#endregion
	}
}