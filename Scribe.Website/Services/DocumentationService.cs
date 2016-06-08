#region References

using System;
using System.Collections.ObjectModel;
using System.Web.Http;
using Scribe.Website.Models;

#endregion

namespace Scribe.Website.Services
{
	public class DocumentationService
	{
		#region Methods

		public PostmanCollection GetDiscovery(string name, Uri uri)
		{
			var explorer = GlobalConfiguration.Configuration.Services.GetApiExplorer();
			var postManCollection = new PostmanCollection();

			postManCollection.Name = name;
			postManCollection.Id = Guid.NewGuid();
			postManCollection.Timestamp = DateTime.Now.Ticks;
			postManCollection.Requests = new Collection<PostmanRequest>();

			foreach (var apiDescription in explorer.ApiDescriptions)
			{
				var relativePath = apiDescription.RelativePath;

				var request = new PostmanRequest
				{
					CollectionId = postManCollection.Id,
					Id = Guid.NewGuid(),
					Method = apiDescription.HttpMethod.Method,
					Url = uri.GetRootDomain().TrimEnd('/') + "/" + relativePath,
					Description = apiDescription.Documentation,
					Name = apiDescription.RelativePath,
					Data = "",
					Headers = "",
					DataMode = "params",
					Timestamp = 0
				};

				postManCollection.Requests.Add(request);
			}

			return postManCollection;
		}

		#endregion
	}
}