#region References

using System;
using System.Reflection;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json.Serialization;

#endregion

namespace Scribe.Website.Hubs
{
	public class SignalRContractResolver : IContractResolver
	{
		#region Fields

		private readonly Assembly _assembly;
		private readonly IContractResolver _camelCaseContractResolver;
		private readonly IContractResolver _defaultContractSerializer;

		#endregion

		#region Constructors

		public SignalRContractResolver()
		{
			_defaultContractSerializer = new DefaultContractResolver();
			_camelCaseContractResolver = new CamelCasePropertyNamesContractResolver();
			_assembly = typeof(Connection).Assembly;
		}

		#endregion

		#region Methods

		public JsonContract ResolveContract(Type type)
		{
			if (type.Assembly.Equals(_assembly))
			{
				return _defaultContractSerializer.ResolveContract(type);
			}

			return _camelCaseContractResolver.ResolveContract(type);
		}

		#endregion
	}
}