#region References

using System;
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
			using (var response = Post($"{_service}DeleteTag/{name}"))
			{
				ValidateResponse(response);
			}
		}

		public FileView GetFile(int id, bool includeData = false)
		{
			using (var response = Get($"{_service}GetFile?id={id}&includeData={includeData}"))
			{
				ValidateResponse(response);
				return Read<FileView>(response);
			}
		}

		public FileView GetFile(string name, bool includeData = false)
		{
			var encoded = HttpUtility.UrlEncode(name);
			using (var response = Get($"{_service}GetFile?name={encoded}&includeData={includeData}"))
			{
				ValidateResponse(response);
				return Read<FileView>(response);
			}
		}

		public PagedResults<FileView> GetFiles(PagedRequest request = null)
		{
			using (var response = Post($"{_service}GetFiles", request))
			{
				ValidateResponse(response);
				return Read<PagedResults<FileView>>(response);
			}
		}

		public PageView GetPage(int id, bool includeHistory = false)
		{
			using (var response = Get($"{_service}GetPage?{nameof(id)}={id}&{nameof(includeHistory)}={includeHistory}"))
			{
				ValidateResponse(response);
				return Read<PageView>(response);
			}
		}

		public PagedResults<PageView> GetPages(PagedRequest request = null)
		{
			using (var response = Post($"{_service}GetPages", request))
			{
				ValidateResponse(response);
				return Read<PagedResults<PageView>>(response);
			}
		}

		public PagedResults<TagView> GetTags(PagedRequest request = null)
		{
			using (var response = Post($"{_service}GetTags", request))
			{
				ValidateResponse(response);
				return Read<PagedResults<TagView>>(response);
			}
		}

		public UserView GetUser(int id)
		{
			using (var response = Get($"{_service}GetUser?{nameof(id)}={id}"))
			{
				ValidateResponse(response);
				return Read<UserView>(response);
			}
		}

		public PagedResults<UserView> GetUsers(PagedRequest request = null)
		{
			using (var response = Post($"{_service}GetUsers", request))
			{
				ValidateResponse(response);
				return Read<PagedResults<UserView>>(response);
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

		public int SaveFile(FileView view)
		{
			using (var response = Post($"{_service}SaveFile", view))
			{
				ValidateResponse(response);
				return Read<int>(response);
			}
		}

		public PageView SavePage(PageView view)
		{
			using (var response = Post($"{_service}SavePage", view))
			{
				ValidateResponse(response);
				return Read<PageView>(response);
			}
		}

		public UserView SaveUser(UserView view)
		{
			using (var response = Post($"{_service}SaveUser", view))
			{
				ValidateResponse(response);
				return Read<UserView>(response);
			}
		}

		public PageView UpdatePage(PageUpdate update)
		{
			using (var response = Post($"{_service}UpdatePage", update))
			{
				ValidateResponse(response);
				return Read<PageView>(response);
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