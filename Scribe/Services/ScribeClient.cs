#region References

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using Scribe.Models.Data;
using Scribe.Models.Views;
using Scribe.Web;

#endregion

namespace Scribe.Services
{
	public class ScribeClient : HttpHelper, IScribeService
	{
		#region Fields

		private readonly string _service;

		#endregion

		#region Constructors

		public ScribeClient(string uri, string service) : base(uri)
		{
			_service = service.EndsWith("/") ? service : service + "/";
		}

		#endregion

		#region Methods

		public void CancelPage(int id)
		{
			using (var response = Post($"{_service}CancelPage/{id}"))
			{
				ValidateResponse(response);
			}
		}

		public void DeleteFile(int id)
		{
			using (var response = Post($"{_service}DeleteFile/{id}"))
			{
				ValidateResponse(response);
			}
		}

		public void DeletePage(int id)
		{
			using (var response = Post($"{_service}DeletePage/{id}"))
			{
				ValidateResponse(response);
			}
		}

		public void DeleteTag(string name)
		{
			using (var response = Post($"{_service}DeletePage/{name}"))
			{
				ValidateResponse(response);
			}
		}

		public FileView GetFile(int id, bool includeData = false)
		{
			using (var response = Get($"{_service}GetFile?id={id}&includeData={includeData}"))
			{
				ValidateResponse(response);
				return Get<FileView>(response);
			}
		}

		public FileView GetFile(string name, bool includeData = false)
		{
			var encoded = HttpUtility.UrlEncode(name);
			using (var response = Get($"{_service}GetFile?name={encoded}&includeData={includeData}"))
			{
				ValidateResponse(response);
				return Get<FileView>(response);
			}
		}

		public IEnumerable<FileView> GetFiles(string filter = null, bool includeData = false)
		{
			var url = $"{_service}GetFiles";

			if (!string.IsNullOrWhiteSpace(filter))
			{
				var encoded = HttpUtility.UrlEncode(filter);
				url += $"?{nameof(filter)}={encoded}&{nameof(includeData)}={includeData}";
			}
			else
			{
				url += $"?{nameof(includeData)}={includeData}";
			}

			using (var response = Get(url))
			{
				ValidateResponse(response);
				return Get<IEnumerable<FileView>>(response);
			}
		}

		public PageView GetPage(int id, bool includeHistory = false)
		{
			using (var response = Get($"{_service}GetPage?{nameof(id)}={id}&{nameof(includeHistory)}={includeHistory}"))
			{
				ValidateResponse(response);
				return Get<PageView>(response);
			}
		}

		public IEnumerable<PageView> GetPages(string filter = null)
		{
			var url = $"{_service}GetPages";

			if (!string.IsNullOrWhiteSpace(filter))
			{
				var encoded = HttpUtility.UrlEncode(filter);
				url += $"?{nameof(filter)}={encoded}";
			}

			using (var response = Get(url))
			{
				ValidateResponse(response);
				return Get<IEnumerable<PageView>>(response);
			}
		}

		public IEnumerable<TagView> GetTags(string filter = null)
		{
			var url = $"{_service}GetTags";

			if (!string.IsNullOrWhiteSpace(filter))
			{
				var encoded = HttpUtility.UrlEncode(filter);
				url += $"?{nameof(filter)}={encoded}";
			}

			using (var response = Get(url))
			{
				ValidateResponse(response);
				return Get<IEnumerable<TagView>>(response);
			}
		}

		public void LogIn(Credentials login)
		{
			using (var response = Post($"{_service}LogIn", login))
			{
				ValidateResponse(response);
			}
		}

		public void LogOut()
		{
			using (var response = Post($"{_service}LogOut"))
			{
				ValidateResponse(response);
			}
		}

		public string Preview(PageView page)
		{
			using (var response = Post($"{_service}Preview", page))
			{
				ValidateResponse(response);
				return response.Content.ReadAsStringAsync().Result;
			}
		}

		public void RenameTag(RenameValues values)
		{
			using (var response = Post($"{_service}RenameTag", values))
			{
				ValidateResponse(response);
			}
		}

		public void SaveFile(FileData data)
		{
			using (var response = Post($"{_service}SaveFile", data))
			{
				ValidateResponse(response);
			}
		}

		public PageView SavePage(PageView view)
		{
			using (var response = Post($"{_service}SavePage", view))
			{
				ValidateResponse(response);
				return Get<PageView>(response);
			}
		}

		private void ValidateResponse(HttpResponseMessage post)
		{
			if (!post.IsSuccessStatusCode)
			{
				throw new Exception(post.ReasonPhrase);
			}
		}

		#endregion
	}
}